namespace HotelReservationSystem.Features.Rooms;

/// <summary>In-memory stand-in until the real API-backed service is wired up.</summary>
public sealed class MockRoomService(MockHotelStore store) : IRoomService
{
    public Task<IReadOnlyList<RoomSummary>> GetRecommendedAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RoomSummary>>(Summaries().Where(r => r.City == "Vaasa").ToList());

    public Task<IReadOnlyList<RoomSummary>> SearchAsync(
        string city, DateTime checkIn, DateTime checkOut, int guests,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RoomSummary>>(Summaries()
            .Where(r => r.City.Equals(city.Trim(), StringComparison.OrdinalIgnoreCase) && r.Capacity >= guests)
            .ToList());

    // Inactive rooms are hidden from guests, as in the backend.
    private IEnumerable<RoomSummary> Summaries() =>
        from room in store.Rooms
        where room.IsActive
        join hotel in store.Hotels on room.HotelId equals hotel.Id
        select new RoomSummary(room.Id, room.RoomType, hotel.Name, hotel.City, room.Capacity, room.PricePerNight);
}
