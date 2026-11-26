using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api.Data;

public enum DatabaseProvider
{
    Postgres,
    MySql
}

public class DatabaseOptions
{
    public const string SectionName = "Database";

    // Postgres or MySql. MySql covers both MySQL and MariaDB.
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.Postgres;

    // MySql only. Tells Pomelo which server it talks to, e.g. "10.4.12-mariadb" or "8.0.36-mysql".
    // Set explicitly instead of auto-detected, so startup doesn't need a database round trip.
    public string? ServerVersion { get; set; }
}

public static class DatabaseSetup
{
    // Registers AppDbContext for the provider chosen in Database:Provider (default Postgres).
    public static IServiceCollection AddAppDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(DatabaseOptions.SectionName);
        DatabaseOptions options;
        try
        {
            options = section.Get<DatabaseOptions>() ?? new DatabaseOptions();
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Database:Provider '{section["Provider"]}' is not supported. Use one of: {string.Join(", ", Enum.GetNames<DatabaseProvider>())}.");
        }

        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:Default must be configured.");

        switch (options.Provider)
        {
            // AppDbContext is what the app injects; the provider-specific type decides which migrations apply.
            case DatabaseProvider.Postgres:
                services.AddDbContext<AppDbContext, PostgresAppDbContext>(o => o.UseNpgsql(connectionString));
                break;

            case DatabaseProvider.MySql:
                if (string.IsNullOrWhiteSpace(options.ServerVersion))
                    throw new InvalidOperationException(
                        "Database:ServerVersion must be set when Database:Provider is MySql, e.g. '10.4.12-mariadb' or '8.0.36-mysql'.");

                ServerVersion serverVersion;
                try
                {
                    serverVersion = ServerVersion.Parse(options.ServerVersion);
                }
                catch (Exception ex) when (ex is FormatException or InvalidOperationException or ArgumentException)
                {
                    throw new InvalidOperationException(
                        $"Database:ServerVersion '{options.ServerVersion}' is not valid. Use a version and type, e.g. '10.4.12-mariadb' or '8.0.36-mysql'.", ex);
                }

                services.AddDbContext<AppDbContext, MySqlAppDbContext>(o => o.UseMySql(connectionString, serverVersion));
                break;
        }

        return services;
    }

    // Npgsql and MySqlConnector errors both derive from DbException, so this works for every provider.
    public static bool IsDatabaseError(Exception ex) =>
        ex is DbException || ex.InnerException is DbException;
}
