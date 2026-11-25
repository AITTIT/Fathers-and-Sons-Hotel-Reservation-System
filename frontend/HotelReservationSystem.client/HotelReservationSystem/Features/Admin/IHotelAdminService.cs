using HotelReservationSystem.Features.Auth;

namespace HotelReservationSystem.Features.Admin;

public sealed record ManagedHotel(int Id, string Name, string AddressLine1, string City, string Country, string? PostalCode);

public sealed record ManagedRoom(int Id, int HotelId, string RoomNumber, string RoomType, int Capacity, decimal PricePerNight, bool IsActive);

public sealed record NewHotel(string Name, string AddressLine1, string City, string Country, string? PostalCode);

public sealed record NewRoom(int HotelId, string RoomNumber, string RoomType, int Capacity, decimal PricePerNight);

/// <summary>
/// Hotel and room administration for admins. The backend already has POST /api/hotels (Admin)
/// and POST /api/rooms (Staff or Admin); swap the mock for an HttpClient-backed service later.
/// </summary>
public interface IHotelAdminService
{
    Task<IReadOnlyList<ManagedHotel>> ListHotelsAsync();

    Task<IReadOnlyList<ManagedRoom>> ListRoomsAsync();

    Task<AuthResult> CreateHotelAsync(NewHotel hotel);

    Task<AuthResult> CreateRoomAsync(NewRoom room);

    Task<AuthResult> RemoveRoomAsync(int roomId);
}
