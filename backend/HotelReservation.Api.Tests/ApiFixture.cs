using HotelReservation.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace HotelReservation.Api.Tests;

public sealed class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:17")
        .Build();

    public CustomWebApplicationFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();
        Factory = new CustomWebApplicationFactory(postgres.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        Factory.Dispose();
        await postgres.DisposeAsync();
    }

    public async Task<int> CreateHotelAsync(string? name = null)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var hotel = new HotelReservation.Api.Features.Hotels.Hotel
        {
            Name = name ?? $"Integration Hotel {Guid.NewGuid():N}",
            AddressLine1 = "1 Test Street",
            City = "Test City",
            Country = "Test Country",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.Hotels.Add(hotel);
        await db.SaveChangesAsync();
        return hotel.Id;
    }
}

public sealed class CustomWebApplicationFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.UseSetting("Jwt:Key", "integration-test-signing-key-0123456789");
        builder.UseSetting("Jwt:Issuer", "HotelReservation.IntegrationTests");
        builder.UseSetting("Jwt:Audience", "HotelReservation.IntegrationTests");
        builder.UseSetting("Seed:Admin:Email", "integration-admin@hotel.test");
        builder.UseSetting("Seed:Admin:Password", "IntegrationAdmin123!");
        builder.UseSetting("Seed:Demo", "false");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
                ["Jwt:Key"] = "integration-test-signing-key-0123456789",
                ["Jwt:Issuer"] = "HotelReservation.IntegrationTests",
                ["Jwt:Audience"] = "HotelReservation.IntegrationTests",
                ["Seed:Admin:Email"] = "integration-admin@hotel.test",
                ["Seed:Admin:Password"] = "IntegrationAdmin123!",
                ["Seed:Demo"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));
        });
    }
}
