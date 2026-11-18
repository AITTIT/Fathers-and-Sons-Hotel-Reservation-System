using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HotelReservation.Api.Data;
using HotelReservation.Api.Features.Auth;
using HotelReservation.Api.Features.Rooms;
using Microsoft.Extensions.DependencyInjection;

namespace HotelReservation.Api.Tests;

public sealed class RoomEndpointTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task GetRooms_ReturnsOnlyActiveRoomsForAnonymousUsers()
    {
        using var client = fixture.Factory.CreateClient();
        var hotelId = await fixture.CreateHotelAsync();

        await AddRoomAsync(hotelId, "101", isActive: true);
        await AddRoomAsync(hotelId, "102", isActive: false);

        var response = await client.GetAsync($"/api/rooms?hotelId={hotelId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = document.RootElement.GetProperty("items");

        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("101", items[0].GetProperty("roomNumber").GetString());
    }

    [Fact]
    public async Task CreateRoom_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var client = fixture.Factory.CreateClient();
        var hotelId = await fixture.CreateHotelAsync();
        var request = new RoomRequest(hotelId, "201", "Standard", 2, 99.00m);

        var response = await client.PostAsJsonAsync("/api/rooms", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateRoom_WithAdminAuthentication_CanBeReadBack()
    {
        using var client = fixture.Factory.CreateClient();
        var hotelId = await fixture.CreateHotelAsync();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await LoginAsAdminAsync(client));

        var request = new RoomRequest(hotelId, "301", "Deluxe", 3, 149.50m);
        var createResponse = await client.PostAsJsonAsync("/api/rooms", request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<RoomResponse>();
        Assert.NotNull(created);

        var getResponse = await client.GetAsync($"/api/rooms/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var persisted = await getResponse.Content.ReadFromJsonAsync<RoomResponse>();
        Assert.NotNull(persisted);
        Assert.Equal(created.Id, persisted.Id);
        Assert.Equal("301", persisted.RoomNumber);
        Assert.Equal("Deluxe", persisted.RoomType);
        Assert.Equal(149.50m, persisted.PricePerNight);
    }

    private async Task<string> LoginAsAdminAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("integration-admin@hotel.test", "IntegrationAdmin123!"));
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        return login.Token;
    }

    private async Task AddRoomAsync(int hotelId, string roomNumber, bool isActive)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        db.Rooms.Add(new Room
        {
            HotelId = hotelId,
            RoomNumber = roomNumber,
            RoomType = "Standard",
            Capacity = 2,
            PricePerNight = 99.00m,
            IsActive = isActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await db.SaveChangesAsync();
    }
}
