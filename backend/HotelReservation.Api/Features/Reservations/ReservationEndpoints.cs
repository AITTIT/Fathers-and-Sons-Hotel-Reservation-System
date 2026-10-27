using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HotelReservation.Api.Data;
using HotelReservation.Api.Features.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HotelReservation.Api.Features.Reservations;

public record ReservationResponse(int Id, int UserId, int HotelId, int RoomId, DateOnly CheckInDate, DateOnly CheckOutDate,
    int GuestCount, string Status, decimal TotalPrice, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

// UserId is only honoured for Staff/Admin booking on behalf of a customer.
public record CreateReservationRequest(int RoomId, DateOnly CheckInDate, DateOnly CheckOutDate, int GuestCount, int? UserId);

public record UpdateReservationRequest(DateOnly CheckInDate, DateOnly CheckOutDate, int GuestCount);

public static class ReservationEndpoints
{
    private static readonly string[] Statuses = ["Pending", "Confirmed", "Cancelled"];

    public static void MapReservationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/reservations").WithTags("Reservations").RequireAuthorization();

        // Customers only ever see their own reservations; others are reported as not found.
        group.MapGet("/", async (int? userId, int? hotelId, int? roomId, string? status, DateOnly? from, DateOnly? to,
            int? page, int? pageSize, ClaimsPrincipal caller, AppDbContext db, CancellationToken ct) =>
        {
            if (status is not null && !Statuses.Contains(status))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = [$"Status must be one of: {string.Join(", ", Statuses)}."] });

            var query = db.Reservations.AsNoTracking().AsQueryable();
            if (!IsStaff(caller))
                query = query.Where(r => r.UserId == CallerId(caller));
            else if (userId is not null)
                query = query.Where(r => r.UserId == userId);
            if (hotelId is not null) query = query.Where(r => r.HotelId == hotelId);
            if (roomId is not null) query = query.Where(r => r.RoomId == roomId);
            if (status is not null) query = query.Where(r => r.Status == status);
            // Overlap with the [from, to) window.
            if (from is not null) query = query.Where(r => r.CheckOutDate > from);
            if (to is not null) query = query.Where(r => r.CheckInDate < to);

            var result = await query.OrderByDescending(r => r.CheckInDate).ThenBy(r => r.Id)
                .Select(r => ToResponse(r)).ToPagedAsync(page, pageSize, ct);
            return Results.Ok(result);
        });

        group.MapGet("/{id:int}", async (int id, ClaimsPrincipal caller, AppDbContext db, CancellationToken ct) =>
        {
            var r = await db.Reservations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            return r is null || !CanAccess(caller, r) ? Results.NotFound() : Results.Ok(ToResponse(r));
        });

        group.MapPost("/", async (CreateReservationRequest request, ClaimsPrincipal caller, AppDbContext db, CancellationToken ct) =>
        {
            var ownerId = IsStaff(caller) && request.UserId is not null ? request.UserId.Value : CallerId(caller);
            if (!await db.Users.AnyAsync(u => u.Id == ownerId, ct))
                return Problem("UserId", "User does not exist.");

            var room = await db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.RoomId && r.IsActive, ct);
            if (room is null)
                return Problem("RoomId", "Room does not exist or is not available.");
            if (ValidateStay(request.CheckInDate, request.CheckOutDate, request.GuestCount, room.Capacity, true) is { } errors)
                return Results.ValidationProblem(errors);
            if (await Overlaps(db, room.Id, request.CheckInDate, request.CheckOutDate, null, ct))
                return Results.Conflict(new { error = "The room is already booked for those dates." });

            var now = DateTime.UtcNow;
            var reservation = new Reservation
            {
                UserId = ownerId,
                HotelId = room.HotelId,
                RoomId = room.Id,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                GuestCount = request.GuestCount,
                Status = "Pending",
                TotalPrice = Price(room.PricePerNight, request.CheckInDate, request.CheckOutDate),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            db.Reservations.Add(reservation);
            return await Save(db, ct, () => Results.Created($"/api/reservations/{reservation.Id}", ToResponse(reservation)));
        });

        // Dates and guest count only; to change room, cancel and book again.
        group.MapPut("/{id:int}", async (int id, UpdateReservationRequest request, ClaimsPrincipal caller, AppDbContext db, CancellationToken ct) =>
        {
            var reservation = await db.Reservations.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (reservation is null || !CanAccess(caller, reservation))
                return Results.NotFound();
            if (reservation.Status == "Cancelled")
                return Results.Conflict(new { error = "Cancelled reservations cannot be changed." });
            if (!IsStaff(caller) && reservation.CheckInDate <= Today())
                return Results.Conflict(new { error = "Reservations that have started can only be changed by staff." });

            var room = await db.Rooms.AsNoTracking().FirstAsync(r => r.Id == reservation.RoomId, ct);
            if (ValidateStay(request.CheckInDate, request.CheckOutDate, request.GuestCount, room.Capacity, !IsStaff(caller)) is { } errors)
                return Results.ValidationProblem(errors);
            if (await Overlaps(db, room.Id, request.CheckInDate, request.CheckOutDate, reservation.Id, ct))
                return Results.Conflict(new { error = "The room is already booked for those dates." });

            reservation.CheckInDate = request.CheckInDate;
            reservation.CheckOutDate = request.CheckOutDate;
            reservation.GuestCount = request.GuestCount;
            reservation.TotalPrice = Price(room.PricePerNight, request.CheckInDate, request.CheckOutDate);
            reservation.UpdatedAtUtc = DateTime.UtcNow;
            return await Save(db, ct, () => Results.Ok(ToResponse(reservation)));
        });

        group.MapPost("/{id:int}/confirm", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var reservation = await db.Reservations.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (reservation is null)
                return Results.NotFound();
            if (reservation.Status != "Pending")
                return Results.Conflict(new { error = $"Only pending reservations can be confirmed (this one is {reservation.Status})." });

            reservation.Status = "Confirmed";
            reservation.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(reservation));
        }).RequireAuthorization(Policies.StaffOrAdmin);

        group.MapPost("/{id:int}/cancel", async (int id, ClaimsPrincipal caller, AppDbContext db, CancellationToken ct) =>
        {
            var reservation = await db.Reservations.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (reservation is null || !CanAccess(caller, reservation))
                return Results.NotFound();
            if (reservation.Status == "Cancelled")
                return Results.Conflict(new { error = "Reservation is already cancelled." });
            if (!IsStaff(caller) && reservation.CheckInDate <= Today())
                return Results.Conflict(new { error = "Reservations that have started can only be cancelled by staff." });

            reservation.Status = "Cancelled";
            reservation.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(reservation));
        });
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);

    private static bool IsStaff(ClaimsPrincipal u) => u.IsInRole("Staff") || u.IsInRole("Admin");

    private static int CallerId(ClaimsPrincipal u) => int.Parse(u.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static bool CanAccess(ClaimsPrincipal u, Reservation r) => IsStaff(u) || r.UserId == CallerId(u);

    private static ReservationResponse ToResponse(Reservation r) => new(r.Id, r.UserId, r.HotelId, r.RoomId, r.CheckInDate,
        r.CheckOutDate, r.GuestCount, r.Status, r.TotalPrice, r.CreatedAtUtc, r.UpdatedAtUtc);

    private static decimal Price(decimal perNight, DateOnly checkIn, DateOnly checkOut) =>
        perNight * (checkOut.DayNumber - checkIn.DayNumber);

    private static IResult Problem(string field, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });

    private static Dictionary<string, string[]>? ValidateStay(DateOnly checkIn, DateOnly checkOut, int guests, int capacity, bool futureOnly)
    {
        var errors = new Dictionary<string, string[]>();
        if (checkOut <= checkIn)
            errors[nameof(checkOut)] = ["Check-out must be after check-in."];
        else if (futureOnly && checkIn < Today())
            errors[nameof(checkIn)] = ["Check-in cannot be in the past."];
        if (guests < 1)
            errors["GuestCount"] = ["At least one guest is required."];
        else if (guests > capacity)
            errors["GuestCount"] = [$"The room holds at most {capacity} guests."];
        return errors.Count > 0 ? errors : null;
    }

    private static Task<bool> Overlaps(AppDbContext db, int roomId, DateOnly checkIn, DateOnly checkOut, int? ignoreId, CancellationToken ct) =>
        db.Reservations.AnyAsync(r => r.RoomId == roomId && r.Status != "Cancelled" && r.Id != ignoreId
            && r.CheckInDate < checkOut && r.CheckOutDate > checkIn, ct);

    // The exclusion constraint is the final guard against two simultaneous bookings.
    private static async Task<IResult> Save(AppDbContext db, CancellationToken ct, Func<IResult> success)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return success();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation })
        {
            return Results.Conflict(new { error = "The room is already booked for those dates." });
        }
    }
}
