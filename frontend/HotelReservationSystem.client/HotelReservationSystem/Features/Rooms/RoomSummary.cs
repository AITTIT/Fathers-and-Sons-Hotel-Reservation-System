namespace HotelReservationSystem.Features.Rooms;

/// <summary>Room as shown in lists. Mirrors the backend Room/Hotel fields the search screens need.</summary>
public sealed record RoomSummary(
    int Id,
    string RoomType,
    string HotelName,
    string City,
    int Capacity,
    decimal PricePerNight);
