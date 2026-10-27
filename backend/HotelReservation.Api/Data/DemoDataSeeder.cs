using HotelReservation.Api.Features.Hotels;
using HotelReservation.Api.Features.Payments;
using HotelReservation.Api.Features.Reservations;
using HotelReservation.Api.Features.Rooms;
using HotelReservation.Api.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api.Data;

// Sample data so frontend work has something to show. Opt in with Seed:Demo=true; never runs outside Development.
public static class DemoDataSeeder
{
    public const string Password = "hoteldemo";

    public static async Task SeedDemoDataAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Hotels.AnyAsync())
            return;

        // All or nothing, so a failure can't leave a half-seeded database that the check above would then skip.
        await using var transaction = await db.Database.BeginTransactionAsync();

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var hash = BCrypt.Net.BCrypt.HashPassword(Password, 11);

        User NewUser(string email, string first, string last, string role) => new()
        {
            Email = email, PasswordHash = hash, FirstName = first, LastName = last, Role = role,
            CreatedAtUtc = now, UpdatedAtUtc = now
        };

        var staff = NewUser("demo-staff@change.me", "Demo", "Staff", "Staff");
        var customer1 = NewUser("demo-customer1@change.me", "Demo", "Customer One", "Customer");
        var customer2 = NewUser("demo-customer2@change.me", "Demo", "Customer Two", "Customer");
        var existing = await db.Users.Select(u => u.Email).ToListAsync();
        db.Users.AddRange(new[] { staff, customer1, customer2 }.Where(u => !existing.Contains(u.Email)));

        (string Name, string Address, string City, string Country, string Postal)[] hotelData =
        [
            ("Grand Harbour Hotel", "1 Harbour Road", "Oslo", "Norway", "0150"),
            ("Alpine Lodge", "12 Mountain Way", "Bergen", "Norway", "5003"),
            ("Seaside Inn", "8 Beach Street", "Copenhagen", "Denmark", "1050")
        ];
        (string Type, int Capacity, decimal Price)[] roomTypes =
            [("Single", 1, 70m), ("Double", 2, 110m), ("Family", 4, 170m), ("Suite", 3, 260m)];

        var hotels = hotelData.Select(h => new Hotel
        {
            Name = h.Name, AddressLine1 = h.Address, City = h.City, Country = h.Country, PostalCode = h.Postal,
            CreatedAtUtc = now, UpdatedAtUtc = now
        }).ToList();
        db.Hotels.AddRange(hotels);
        await db.SaveChangesAsync();

        var rooms = new List<Room>();
        for (var h = 0; h < hotels.Count; h++)
            for (var r = 0; r < roomTypes.Length; r++)
                for (var n = 1; n <= 2; n++)
                    rooms.Add(new Room
                    {
                        HotelId = hotels[h].Id, RoomNumber = $"{r + 1}0{n}", RoomType = roomTypes[r].Type,
                        Capacity = roomTypes[r].Capacity, PricePerNight = roomTypes[r].Price,
                        IsActive = !(h == 2 && r == 3 && n == 2), // one inactive room to exercise the filter
                        CreatedAtUtc = now, UpdatedAtUtc = now
                    });
        db.Rooms.AddRange(rooms);
        await db.SaveChangesAsync();

        Reservation Book(User user, Room room, int startInDays, int nights, int guests, string status) => new()
        {
            UserId = user.Id, HotelId = room.HotelId, RoomId = room.Id,
            CheckInDate = today.AddDays(startInDays), CheckOutDate = today.AddDays(startInDays + nights),
            GuestCount = guests, Status = status, TotalPrice = room.PricePerNight * nights,
            CreatedAtUtc = now, UpdatedAtUtc = now
        };

        var confirmed = Book(customer1, rooms[2], 7, 3, 2, "Confirmed");   // Double
        var pending = Book(customer2, rooms[4], 14, 2, 2, "Pending");      // Family
        var cancelled = Book(customer1, rooms[5], 21, 4, 3, "Cancelled");  // Family
        db.Reservations.AddRange(confirmed, pending, cancelled);
        await db.SaveChangesAsync();

        db.Payments.AddRange(
            new Payment
            {
                ReservationId = confirmed.Id, Provider = "Visa", ProviderTransactionId = "demo-txn-1", Amount = confirmed.TotalPrice,
                Status = "Succeeded", PaidAtUtc = now, CreatedAtUtc = now
            },
            new Payment
            {
                ReservationId = pending.Id, Provider = "PayPal", Amount = pending.TotalPrice,
                Status = "Pending", CreatedAtUtc = now
            });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        logger.LogInformation("Seeded demo data: 3 hotels, {Rooms} rooms, 3 demo users, 3 reservations, 2 payments.", rooms.Count);
    }
}
