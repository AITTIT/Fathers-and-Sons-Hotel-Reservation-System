using HotelReservation.Api.Features.Auth;
using HotelReservation.Api.Features.Hotels;
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

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Hotel> Hotels => Set<Hotel>();

    public DbSet<Room> Rooms => Set<Room>();

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

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(t => t.TokenHash).IsUnique();
            e.HasIndex(t => t.UserId);
        });

        modelBuilder.Entity<Room>(e =>
        {
            e.Property(r => r.PricePerNight).HasPrecision(10, 2);
            e.HasOne(r => r.Hotel).WithMany(h => h.Rooms).HasForeignKey(r => r.HotelId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(r => new { r.HotelId, r.RoomNumber }).IsUnique();
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Rooms_Capacity", "\"Capacity\" > 0");
                t.HasCheckConstraint("CK_Rooms_Price", "\"PricePerNight\" >= 0");
            });
        });
    }
}