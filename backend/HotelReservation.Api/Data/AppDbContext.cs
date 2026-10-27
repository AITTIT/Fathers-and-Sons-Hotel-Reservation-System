using HotelReservation.Api.Features.Hotels;
using HotelReservation.Api.Features.Payments;
using HotelReservation.Api.Features.Reservations;
using HotelReservation.Api.Features.Rooms;
using HotelReservation.Api.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<Hotel> Hotels => Set<Hotel>();

    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_Users_Role", "\"Role\" IN ('Customer', 'Staff', 'Admin')"));
        });

        modelBuilder.Entity<PasswordResetToken>(e =>
        {
            e.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(t => t.TokenHash).IsUnique();
        });

        modelBuilder.Entity<Room>(e =>
        {
            e.Property(r => r.PricePerNight).HasPrecision(10, 2);
            e.HasOne<Hotel>().WithMany().HasForeignKey(r => r.HotelId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(r => new { r.HotelId, r.RoomNumber }).IsUnique();
            // Lets reservations reference (RoomId, HotelId) so a reservation's hotel always matches its room's.
            e.HasAlternateKey(r => new { r.Id, r.HotelId }).HasName("UQ_Rooms_Id_HotelId");
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Rooms_Capacity", "\"Capacity\" > 0");
                t.HasCheckConstraint("CK_Rooms_Price", "\"PricePerNight\" >= 0");
            });
        });

        modelBuilder.Entity<Reservation>(e =>
        {
            e.Property(r => r.TotalPrice).HasPrecision(10, 2);
            e.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Hotel>().WithMany().HasForeignKey(r => r.HotelId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Room>().WithMany().HasForeignKey(r => new { r.RoomId, r.HotelId })
                .HasPrincipalKey(r => new { r.Id, r.HotelId }).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(r => r.UserId);
            e.HasIndex(r => r.RoomId);
            e.HasIndex(r => r.HotelId);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Reservations_Dates", "\"CheckInDate\" < \"CheckOutDate\"");
                t.HasCheckConstraint("CK_Reservations_GuestCount", "\"GuestCount\" > 0");
                t.HasCheckConstraint("CK_Reservations_Status", "\"Status\" IN ('Pending', 'Confirmed', 'Cancelled')");
                t.HasCheckConstraint("CK_Reservations_TotalPrice", "\"TotalPrice\" >= 0");
            });
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.Property(p => p.Amount).HasPrecision(10, 2);
            e.HasOne<Reservation>().WithMany().HasForeignKey(p => p.ReservationId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(p => p.ReservationId);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Payments_Amount", "\"Amount\" >= 0");
                t.HasCheckConstraint("CK_Payments_Status", "\"Status\" IN ('Pending', 'Succeeded', 'Failed')");
                t.HasCheckConstraint("CK_Payments_PaidAt", "(\"Status\" = 'Succeeded') = (\"PaidAtUtc\" IS NOT NULL)");
            });
        });
    }
}