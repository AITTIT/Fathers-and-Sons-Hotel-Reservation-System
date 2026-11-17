namespace HotelReservationSystem.Features.Rooms;

/// <summary>In-memory stand-in until the real API-backed service is wired up.</summary>
public sealed class MockRoomService : IRoomService
{
    private static readonly IReadOnlyList<RoomSummary> Rooms =
    [
        new(1, "Double", "Hotel Rantakatu", "Vaasa", 2, 89m),
        new(2, "Suite", "Hotel Keskusta", "Vaasa", 4, 159m),
        new(3, "Single", "Hotel Rantakatu", "Vaasa", 1, 59m),
        new(4, "Double", "Hotel Satama", "Helsinki", 2, 129m),
    ];

    public Task<IReadOnlyList<RoomSummary>> GetRecommendedAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RoomSummary>>(Rooms.Where(r => r.City == "Vaasa").ToList());

    public Task<IReadOnlyList<RoomSummary>> SearchAsync(
        string city, DateTime checkIn, DateTime checkOut, int guests,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RoomSummary>>(Rooms
            .Where(r => r.City.Equals(city.Trim(), StringComparison.OrdinalIgnoreCase) && r.Capacity >= guests)
            .ToList());
}
