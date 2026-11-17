namespace HotelReservationSystem.Features.Rooms;

/// <summary>API-facing abstraction for rooms. ViewModels depend on this, never on HttpClient directly.</summary>
public interface IRoomService
{
    Task<IReadOnlyList<RoomSummary>> GetRecommendedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoomSummary>> SearchAsync(
        string city, DateTime checkIn, DateTime checkOut, int guests,
        CancellationToken cancellationToken = default);
}
