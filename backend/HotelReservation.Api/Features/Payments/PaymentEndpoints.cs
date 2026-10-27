using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HotelReservation.Api.Data;
using HotelReservation.Api.Features.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api.Features.Payments;

public record PaymentResponse(int Id, int ReservationId, string Provider, string? ProviderTransactionId, decimal Amount,
    string Currency, string Status, DateTime? PaidAtUtc, DateTime CreatedAtUtc);

public record CreatePaymentRequest(int ReservationId, string Provider);

// Stands in for the payment provider's callback until a real provider is integrated.
public record CompletePaymentRequest(bool Succeeded, string? ProviderTransactionId);

public static class PaymentEndpoints
{
    private static readonly string[] Providers = ["Visa", "PayPal", "MobilePay"];
    private static readonly string[] Statuses = ["Pending", "Succeeded", "Failed"];

    public static void MapPaymentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/payments").WithTags("Payments").RequireAuthorization();

        // Customers only see payments for their own reservations; others are reported as not found.
        group.MapGet("/", async (int? reservationId, string? status, int? page, int? pageSize,
            ClaimsPrincipal caller, AppDbContext db, CancellationToken ct) =>
        {
            if (status is not null && !Statuses.Contains(status))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = [$"Status must be one of: {string.Join(", ", Statuses)}."] });

            var query = db.Payments.AsNoTracking().AsQueryable();
            if (!IsStaff(caller))
            {
                var me = CallerId(caller);
                query = query.Where(p => db.Reservations.Any(r => r.Id == p.ReservationId && r.UserId == me));
            }
            if (reservationId is not null) query = query.Where(p => p.ReservationId == reservationId);
            if (status is not null) query = query.Where(p => p.Status == status);

            var result = await query.OrderByDescending(p => p.CreatedAtUtc).ThenByDescending(p => p.Id)
                .Select(p => ToResponse(p)).ToPagedAsync(page, pageSize, ct);
            return Results.Ok(result);
        })
        .WithSummary("List payments")
        .WithDescription("Requires login. Customers only see payments for their own reservations. Filters: `reservationId`, `status`. Paged.");

        group.MapGet("/{id:int}", async (int id, ClaimsPrincipal caller, AppDbContext db, CancellationToken ct) =>
        {
            var payment = await db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
            return payment is null || !await CanAccess(caller, payment, db, ct) ? Results.NotFound() : Results.Ok(ToResponse(payment));
                    })
                    .WithSummary("Get a payment")
                    .WithDescription("Requires login. Customers get 404 for other people's payments.");

                    // Starts a payment for whatever is still owed on the reservation. Amount is always computed server-side.
        group.MapPost("/", async (CreatePaymentRequest request, ClaimsPrincipal caller, AppDbContext db, CancellationToken ct) =>
        {
            var reservation = await db.Reservations.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.ReservationId, ct);
            if (reservation is null || !(IsStaff(caller) || reservation.UserId == CallerId(caller)))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["ReservationId"] = ["Reservation does not exist."] });
            if (!Providers.Contains(request.Provider))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["Provider"] = [$"Provider must be one of: {string.Join(", ", Providers)}."] });
            if (reservation.Status == "Cancelled")
                return Results.Conflict(new { error = "Cancelled reservations cannot be paid." });
            if (await db.Payments.AnyAsync(p => p.ReservationId == reservation.Id && p.Status == "Pending", ct))
                return Results.Conflict(new { error = "This reservation already has a pending payment." });

            var paid = await db.Payments.Where(p => p.ReservationId == reservation.Id && p.Status == "Succeeded").SumAsync(p => p.Amount, ct);
            var outstanding = reservation.TotalPrice - paid;
            if (outstanding <= 0)
                return Results.Conflict(new { error = "This reservation is already paid in full." });

            var payment = new Payment
            {
                ReservationId = reservation.Id,
                Provider = request.Provider,
                Amount = outstanding,
                Currency = "EUR",
                Status = "Pending",
                CreatedAtUtc = DateTime.UtcNow
            };
            db.Payments.Add(payment);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/payments/{payment.Id}", ToResponse(payment));
        })
        .WithSummary("Start a payment for a reservation")
        .WithDescription("Requires login (owner or Staff/Admin). The amount is the amount still owed, in EUR, and is set by the server.");

        // A successful payment confirms a pending reservation.
        group.MapPost("/{id:int}/complete", async (int id, CompletePaymentRequest request, AppDbContext db, CancellationToken ct) =>
        {
            var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (payment is null)
                return Results.NotFound();
            if (payment.Status != "Pending")
                return Results.Conflict(new { error = $"Only pending payments can be completed (this one is {payment.Status})." });

            payment.ProviderTransactionId = string.IsNullOrWhiteSpace(request.ProviderTransactionId) ? null : request.ProviderTransactionId.Trim();
            if (request.Succeeded)
            {
                payment.Status = "Succeeded";
                payment.PaidAtUtc = DateTime.UtcNow;
                var reservation = await db.Reservations.FirstAsync(r => r.Id == payment.ReservationId, ct);
                if (reservation.Status == "Pending")
                {
                    reservation.Status = "Confirmed";
                    reservation.UpdatedAtUtc = DateTime.UtcNow;
                }
            }
            else
            {
                payment.Status = "Failed";
            }
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(payment));
        }).RequireAuthorization(Policies.StaffOrAdmin)
        .WithSummary("Mark a payment succeeded or failed")
        .WithDescription("Requires Staff or Admin. Simulates the payment provider's callback. Success confirms a pending reservation.");

        group.MapPost("/{id:int}/cancel", async (int id, ClaimsPrincipal caller, AppDbContext db, CancellationToken ct) =>
        {
            var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (payment is null || !await CanAccess(caller, payment, db, ct))
                return Results.NotFound();
            if (payment.Status != "Pending")
                return Results.Conflict(new { error = $"Only pending payments can be cancelled (this one is {payment.Status})." });

            payment.Status = "Failed";
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(payment));
        })
        .WithSummary("Cancel a pending payment")
        .WithDescription("Requires login (owner or Staff/Admin).");
    }

    private static bool IsStaff(ClaimsPrincipal u) => u.IsInRole("Staff") || u.IsInRole("Admin");

    private static int CallerId(ClaimsPrincipal u) => int.Parse(u.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<bool> CanAccess(ClaimsPrincipal u, Payment p, AppDbContext db, CancellationToken ct)
    {
        if (IsStaff(u)) return true;
        var me = CallerId(u);
        return await db.Reservations.AnyAsync(r => r.Id == p.ReservationId && r.UserId == me, ct);
    }

    private static PaymentResponse ToResponse(Payment p) => new(p.Id, p.ReservationId, p.Provider, p.ProviderTransactionId,
        p.Amount, p.Currency, p.Status, p.PaidAtUtc, p.CreatedAtUtc);
}
