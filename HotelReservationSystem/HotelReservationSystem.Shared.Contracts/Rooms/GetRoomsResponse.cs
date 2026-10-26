namespace HotelReservationSystem.Shared.Contracts.Rooms;

public sealed record GetRoomsResponse(IReadOnlyList<RoomSummaryDto> Rooms);
