using HotelReservation.Api.Data;
using HotelReservation.Api.Features.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api.Features.Rooms;

public record RoomResponse(int Id, int HotelId, string RoomNumber, string RoomType, int Capacity, decimal PricePerNight, bool IsActive);

public record RoomRequest(int HotelId, string RoomNumber, string RoomType, int Capacity, decimal PricePerNight, bool IsActive = true);

public static class RoomEndpoints
{
    public static void MapRoomEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rooms").WithTags("Rooms");

        // Inactive rooms are only visible to staff and admins.
        group.MapGet("/", async (int? hotelId, string? roomType, int? minCapacity, decimal? maxPrice, bool? includeInactive,
            int? page, int? pageSize, HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.Rooms.AsNoTracking().AsQueryable();
            if (!(includeInactive == true && IsStaff(http)))
                query = query.Where(r => r.IsActive);
            if (hotelId is not null) query = query.Where(r => r.HotelId == hotelId);
            if (!string.IsNullOrWhiteSpace(roomType)) query = query.Where(r => r.RoomType.ToLower() == roomType.Trim().ToLower());
            if (minCapacity is not null) query = query.Where(r => r.Capacity >= minCapacity);
            if (maxPrice is not null) query = query.Where(r => r.PricePerNight <= maxPrice);

            var result = await query.OrderBy(r => r.HotelId).ThenBy(r => r.RoomNumber)
                .Select(r => ToResponse(r)).ToPagedAsync(page, pageSize, ct);
            return Results.Ok(result);
        })
        .WithSummary("List rooms")
        .WithDescription("Public. Filters: `hotelId`, `roomType`, `minCapacity`, `maxPrice`. Inactive rooms are only returned to Staff/Admin with `includeInactive=true`. Paged.");

        group.MapGet("/{id:int}", async (int id, HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var staff = IsStaff(http);
            var room = await db.Rooms.AsNoTracking().Where(r => r.Id == id && (r.IsActive || staff))
                .Select(r => ToResponse(r)).FirstOrDefaultAsync(ct);
            return room is null ? Results.NotFound() : Results.Ok(room);
        })
        .WithSummary("Get a room")
        .WithDescription("Public. Inactive rooms are hidden from non-staff (404).");

        group.MapPost("/", async (RoomRequest request, AppDbContext db, CancellationToken ct) =>
        {
            if (Validate(request) is { } errors)
                return Results.ValidationProblem(errors);
            if (!await db.Hotels.AnyAsync(h => h.Id == request.HotelId, ct))
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.HotelId)] = ["Hotel does not exist."] });
            if (await db.Rooms.AnyAsync(r => r.HotelId == request.HotelId && r.RoomNumber == request.RoomNumber.Trim(), ct))
                return Results.Conflict(new { error = "Room number already exists in this hotel." });

            var now = DateTime.UtcNow;
            var room = new Room { HotelId = request.HotelId, CreatedAtUtc = now, UpdatedAtUtc = now };
            Apply(room, request);
            db.Rooms.Add(room);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/rooms/{room.Id}", ToResponse(room));
        }).RequireAuthorization(Policies.StaffOrAdmin)
        .WithSummary("Create a room")
        .WithDescription("Requires Staff or Admin. `roomNumber` must be unique within the hotel.");

        // A room cannot move between hotels, because existing reservations reference (RoomId, HotelId).
        group.MapPut("/{id:int}", async (int id, RoomRequest request, AppDbContext db, CancellationToken ct) =>
        {
            if (Validate(request) is { } errors)
                return Results.ValidationProblem(errors);

            var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (room is null)
                return Results.NotFound();
            if (request.HotelId != room.HotelId)
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.HotelId)] = ["A room cannot be moved to another hotel."] });

            var number = request.RoomNumber.Trim();
            if (number != room.RoomNumber && await db.Rooms.AnyAsync(r => r.HotelId == room.HotelId && r.RoomNumber == number, ct))
                return Results.Conflict(new { error = "Room number already exists in this hotel." });

            Apply(room, request);
            room.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(room));
        }).RequireAuthorization(Policies.StaffOrAdmin)
        .WithSummary("Update a room")
        .WithDescription("Requires Staff or Admin. A room cannot be moved to another hotel.");

        group.MapDelete("/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (room is null)
                return Results.NotFound();

            db.Rooms.Remove(room);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { error = "Room has reservations and cannot be deleted. Deactivate it instead." });
            }
            return Results.NoContent();
        }).RequireAuthorization(Policies.Admin)
        .WithSummary("Delete a room")
        .WithDescription("Requires Admin. 409 if it has reservations; deactivate it instead.");
    }

    private static bool IsStaff(HttpContext http) => http.User.IsInRole("Staff") || http.User.IsInRole("Admin");

    private static RoomResponse ToResponse(Room r) => new(r.Id, r.HotelId, r.RoomNumber, r.RoomType, r.Capacity, r.PricePerNight, r.IsActive);

    private static void Apply(Room room, RoomRequest r)
    {
        room.RoomNumber = r.RoomNumber.Trim();
        room.RoomType = r.RoomType.Trim();
        room.Capacity = r.Capacity;
        room.PricePerNight = r.PricePerNight;
        room.IsActive = r.IsActive;
    }

    private static Dictionary<string, string[]>? Validate(RoomRequest r)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(r.RoomNumber)) errors[nameof(r.RoomNumber)] = ["Room number is required."];
        if (string.IsNullOrWhiteSpace(r.RoomType)) errors[nameof(r.RoomType)] = ["Room type is required."];
        if (r.Capacity < 1) errors[nameof(r.Capacity)] = ["Capacity must be at least 1."];
        if (r.PricePerNight < 0) errors[nameof(r.PricePerNight)] = ["Price cannot be negative."];
        return errors.Count > 0 ? errors : null;
    }
}
