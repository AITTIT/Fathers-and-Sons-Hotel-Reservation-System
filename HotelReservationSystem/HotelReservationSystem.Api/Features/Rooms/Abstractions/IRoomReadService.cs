using HotelReservationSystem.Shared.Contracts.Rooms;

namespace HotelReservationSystem.Api.Features.Rooms.Abstractions;

public interface IRoomReadService
{
    Task<IReadOnlyList<RoomSummaryDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
