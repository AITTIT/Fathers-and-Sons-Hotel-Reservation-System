using HotelReservation.Api.Features.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HotelReservation.Api.Data;

public class AdminSeedOptions
{
    public const string SectionName = "Seed:Admin";

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FirstName { get; set; } = "Admin";

    public string LastName { get; set; } = "User";
}

public static class DatabaseInitializer
{
    private const int MaxAttempts = 10;

    // Applies pending migrations (waiting for the database to accept connections), then makes sure an admin exists.
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync();
                break;
            }
            catch (Exception ex) when (attempt < MaxAttempts && (ex is NpgsqlException || ex.InnerException is NpgsqlException))
            {
                logger.LogWarning("Database not ready (attempt {Attempt}/{Max}): {Message}", attempt, MaxAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        await SeedAdminAsync(db, app.Configuration.GetSection(AdminSeedOptions.SectionName).Get<AdminSeedOptions>() ?? new(), logger);
    }

    // Only runs while the system has no admin, and never overwrites an existing account.
    private static async Task SeedAdminAsync(AppDbContext db, AdminSeedOptions options, ILogger logger)
    {
        if (await db.Users.AnyAsync(u => u.Role == "Admin"))
            return;

        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrEmpty(options.Password))
        {
            logger.LogWarning("There is no admin account. Set Seed:Admin:Email and Seed:Admin:Password to create one at startup.");
            return;
        }
        if (AuthEndpoints.PasswordError(options.Password) is { } passwordError)
            throw new InvalidOperationException($"Seed:Admin:Password is invalid: {passwordError}");

        var email = options.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException($"Cannot create the admin: {email} is already registered with another role.");

        var now = DateTime.UtcNow;
        db.Users.Add(new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(options.Password, 11),
            FirstName = options.FirstName.Trim(),
            LastName = options.LastName.Trim(),
            Role = "Admin",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Created admin account {Email}.", email);
    }
}
