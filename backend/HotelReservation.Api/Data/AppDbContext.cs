using HotelReservation.Api.Features.Auth;
using HotelReservation.Api.Features.Hotels;
using HotelReservation.Api.Features.Rooms;
using HotelReservation.Api.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HotelReservation.Api.Data;

public class AppDbContext : DbContext
{
    // Matches the 254-character limit the endpoints already enforce.
    public const int EmailMaxLength = 254;

    // Token hashes are hex SHA-256, always 64 characters.
    public const int TokenHashMaxLength = 64;

    public const string MySqlCharSet = "utf8mb4";

    public const string MySqlCollation = "utf8mb4_unicode_520_ci";

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Used by the provider-specific contexts in ProviderDbContexts.cs.
    protected AppDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Hotel> Hotels => Set<Hotel>();

    public DbSet<Room> Rooms => Set<Room>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // PostgreSQL's timestamp with time zone already returns UTC values. MySQL/MariaDB datetime has no time zone,
        // so values come back with an unspecified kind; mark them as UTC so the API still sends timestamps ending in Z.
        if (Database.IsMySql())
        {
            configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
            configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Full Unicode with a consistent collation, whatever the MySQL/MariaDB server's defaults are (often latin1).
        if (Database.IsMySql())
            modelBuilder.HasCharSet(MySqlCharSet).UseCollation(MySqlCollation);

        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.Email).HasMaxLength(EmailMaxLength);
            e.HasIndex(u => u.Email).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_Users_Role", $"{Column("Role")} IN ('Customer', 'Staff', 'Admin')"));
        });

        modelBuilder.Entity<PasswordResetToken>(e =>
        {
            e.Property(t => t.TokenHash).HasMaxLength(TokenHashMaxLength);
            e.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(t => t.TokenHash).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.Property(t => t.TokenHash).HasMaxLength(TokenHashMaxLength);
            e.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(t => t.TokenHash).IsUnique();
            e.HasIndex(t => t.UserId);
        });

        modelBuilder.Entity<Room>(e =>
        {
            e.Property(r => r.RoomNumber).HasMaxLength(Room.RoomNumberMaxLength);
            e.Property(r => r.PricePerNight).HasPrecision(10, 2);
            e.HasOne(r => r.Hotel).WithMany(h => h.Rooms).HasForeignKey(r => r.HotelId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(r => new { r.HotelId, r.RoomNumber }).IsUnique();
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Rooms_Capacity", $"{Column("Capacity")} > 0");
                t.HasCheckConstraint("CK_Rooms_Price", $"{Column("PricePerNight")} >= 0");
            });
        });
    }

    // Check constraints are raw SQL, so column names need the provider's own quoting:
    // "Name" in PostgreSQL, `Name` in MySQL/MariaDB (where "Name" would be a string literal).
    private string Column(string name) => Database.IsMySql() ? $"`{name}`" : $"\"{name}\"";

    private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter()
            : base(
                v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v,
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }
}
