using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HotelReservation.Api.Data;

// One context type per database provider, so each provider has its own migrations and model snapshot
// (Migrations/Postgres and Migrations/MySql). The rest of the app only uses AppDbContext.
// Regenerate both migration sets with backend/scripts/migrations.ps1.

public sealed class PostgresAppDbContext(DbContextOptions<PostgresAppDbContext> options) : AppDbContext(options);

public sealed class MySqlAppDbContext(DbContextOptions<MySqlAppDbContext> options) : AppDbContext(options);

// Design-time factories: `dotnet ef` uses these instead of starting the app, so creating or checking
// migrations needs no database, secrets or configuration. Pass --connection to target a real database.

public sealed class PostgresDesignTimeFactory : IDesignTimeDbContextFactory<PostgresAppDbContext>
{
    // Matches the local docker-compose database.
    private const string DefaultConnection = "Host=localhost;Port=5432;Database=hotel_reservation;Username=postgres;Password=postgres";

    public PostgresAppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<PostgresAppDbContext>()
            .UseNpgsql(DefaultConnection)
            .Options);
}

public sealed class MySqlDesignTimeFactory : IDesignTimeDbContextFactory<MySqlAppDbContext>
{
    private const string DefaultConnection = "Server=localhost;Port=3306;Database=hotel_reservation;User=root;Password=root";

    // Migrations are generated for the oldest supported server. When applying them with `dotnet ef database update`,
    // set Database__ServerVersion to the real server (scripts/migrations.ps1 -ServerVersion does this).
    private const string MinimumServerVersion = "10.4.12-mariadb";

    public MySqlAppDbContext CreateDbContext(string[] args)
    {
        var version = Environment.GetEnvironmentVariable("Database__ServerVersion");
        return new(new DbContextOptionsBuilder<MySqlAppDbContext>()
            .UseMySql(DefaultConnection, ServerVersion.Parse(string.IsNullOrWhiteSpace(version) ? MinimumServerVersion : version))
            .Options);
    }
}
