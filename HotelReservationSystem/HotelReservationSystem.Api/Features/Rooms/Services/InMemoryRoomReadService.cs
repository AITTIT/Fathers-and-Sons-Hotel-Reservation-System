using HotelReservationSystem.Api.Features.Rooms.Abstractions;
using HotelReservationSystem.Shared.Contracts.Rooms;

namespace HotelReservationSystem.Api.Features.Rooms.Services;

public sealed class InMemoryRoomReadService : IRoomReadService
{
    private static readonly IReadOnlyList<RoomSummaryDto> Rooms =
    [
        new(1, 1, "101", "Single", 1, 89.00m),
        new(2, 1, "102", "Double", 2, 129.00m),
        new(3, 1, "201", "Suite", 4, 249.00m)
    ];

    public Task<IReadOnlyList<RoomSummaryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Rooms);
    }
}
