using HotelReservation.Api.Data;
using HotelReservation.Api.Features.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api.Features.Hotels;

public record HotelResponse(int Id, string Name, string AddressLine1, string City, string Country, string? PostalCode);

public record HotelRequest(string Name, string AddressLine1, string City, string Country, string? PostalCode);

public static class HotelEndpoints
{
    public static void MapHotelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/hotels").WithTags("Hotels");

        group.MapGet("/", async (string? city, string? country, int? page, int? pageSize, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.Hotels.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(city))
                query = query.Where(h => h.City.ToLower() == city.Trim().ToLower());
            if (!string.IsNullOrWhiteSpace(country))
                query = query.Where(h => h.Country.ToLower() == country.Trim().ToLower());

            var result = await query.OrderBy(h => h.Id).Select(h => ToResponse(h)).ToPagedAsync(page, pageSize, ct);
            return Results.Ok(result);
        })
        .WithSummary("List hotels")
        .WithDescription("Public. Filters: `city`, `country`. Paged.");

        group.MapGet("/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var hotel = await db.Hotels.AsNoTracking().Where(h => h.Id == id).Select(h => ToResponse(h)).FirstOrDefaultAsync(ct);
            return hotel is null ? Results.NotFound() : Results.Ok(hotel);
        })
        .WithSummary("Get a hotel")
        .WithDescription("Public.");

        group.MapPost("/", async (HotelRequest request, AppDbContext db, CancellationToken ct) =>
        {
            if (Validate(request) is { } errors)
                return Results.ValidationProblem(errors);

            var now = DateTime.UtcNow;
            var hotel = new Hotel { CreatedAtUtc = now, UpdatedAtUtc = now };
            Apply(hotel, request);
            db.Hotels.Add(hotel);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/hotels/{hotel.Id}", ToResponse(hotel));
        }).RequireAuthorization(Policies.Admin)
        .WithSummary("Create a hotel")
        .WithDescription("Requires Admin.");

        group.MapPut("/{id:int}", async (int id, HotelRequest request, AppDbContext db, CancellationToken ct) =>
        {
            if (Validate(request) is { } errors)
                return Results.ValidationProblem(errors);

            var hotel = await db.Hotels.FirstOrDefaultAsync(h => h.Id == id, ct);
            if (hotel is null)
                return Results.NotFound();

            Apply(hotel, request);
            hotel.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(hotel));
        }).RequireAuthorization(Policies.Admin)
        .WithSummary("Update a hotel")
        .WithDescription("Requires Admin.");

        group.MapDelete("/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var hotel = await db.Hotels.FirstOrDefaultAsync(h => h.Id == id, ct);
            if (hotel is null)
                return Results.NotFound();

            db.Hotels.Remove(hotel);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { error = "Hotel has rooms with reservations and cannot be deleted." });
            }
            return Results.NoContent();
        }).RequireAuthorization(Policies.Admin)
        .WithSummary("Delete a hotel")
        .WithDescription("Requires Admin. Also deletes its rooms. 409 if any room has reservations.");
    }

    private static HotelResponse ToResponse(Hotel h) => new(h.Id, h.Name, h.AddressLine1, h.City, h.Country, h.PostalCode);

    private static void Apply(Hotel hotel, HotelRequest r)
    {
        hotel.Name = r.Name.Trim();
        hotel.AddressLine1 = r.AddressLine1.Trim();
        hotel.City = r.City.Trim();
        hotel.Country = r.Country.Trim();
        hotel.PostalCode = string.IsNullOrWhiteSpace(r.PostalCode) ? null : r.PostalCode.Trim();
    }

    private static Dictionary<string, string[]>? Validate(HotelRequest r)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(r.Name)) errors[nameof(r.Name)] = ["Name is required."];
        if (string.IsNullOrWhiteSpace(r.AddressLine1)) errors[nameof(r.AddressLine1)] = ["Address is required."];
        if (string.IsNullOrWhiteSpace(r.City)) errors[nameof(r.City)] = ["City is required."];
        if (string.IsNullOrWhiteSpace(r.Country)) errors[nameof(r.Country)] = ["Country is required."];
        return errors.Count > 0 ? errors : null;
    }
}
