namespace HotelReservationSystem.Shared.Contracts.Rooms;

public sealed record RoomSummaryDto(
    int Id,
    int HotelId,
    string RoomNumber,
    string RoomType,
    int Capacity,
    decimal PricePerNight);
