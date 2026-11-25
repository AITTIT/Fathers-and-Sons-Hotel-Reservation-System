using HotelReservationSystem.Features.Auth;
using HotelReservationSystem.Features.Rooms;

namespace HotelReservationSystem.Features.Admin;

/// <summary>In-memory stand-in until the API client exists. Validation mirrors the backend endpoints.</summary>
public sealed class MockHotelAdminService(MockHotelStore store, IAuthService auth) : IHotelAdminService
{
    public Task<IReadOnlyList<ManagedHotel>> ListHotelsAsync()
    {
        IReadOnlyList<ManagedHotel> hotels = IsAdmin
            ? store.Hotels
                .Select(h => new ManagedHotel(h.Id, h.Name, h.AddressLine1, h.City, h.Country, h.PostalCode))
                .ToList()
            : [];
        return Task.FromResult(hotels);
    }

    public Task<IReadOnlyList<ManagedRoom>> ListRoomsAsync()
    {
        IReadOnlyList<ManagedRoom> rooms = IsAdmin
            ? store.Rooms
                .Select(r => new ManagedRoom(r.Id, r.HotelId, r.RoomNumber, r.RoomType, r.Capacity, r.PricePerNight, r.IsActive))
                .ToList()
            : [];
        return Task.FromResult(rooms);
    }

    public Task<AuthResult> CreateHotelAsync(NewHotel hotel)
    {
        if (!IsAdmin)
            return NotAllowed();

        if (string.IsNullOrWhiteSpace(hotel.Name)) return Fail("Name is required.");
        if (string.IsNullOrWhiteSpace(hotel.AddressLine1)) return Fail("Address is required.");
        if (string.IsNullOrWhiteSpace(hotel.City)) return Fail("City is required.");
        if (string.IsNullOrWhiteSpace(hotel.Country)) return Fail("Country is required.");

        store.AddHotel(hotel.Name.Trim(), hotel.AddressLine1.Trim(), hotel.City.Trim(), hotel.Country.Trim(),
            string.IsNullOrWhiteSpace(hotel.PostalCode) ? null : hotel.PostalCode.Trim());
        return Ok();
    }

    public Task<AuthResult> CreateRoomAsync(NewRoom room)
    {
        if (!IsAdmin)
            return NotAllowed();

        if (string.IsNullOrWhiteSpace(room.RoomNumber)) return Fail("Room number is required.");
        if (string.IsNullOrWhiteSpace(room.RoomType)) return Fail("Room type is required.");
        if (room.Capacity < 1) return Fail("Capacity must be at least 1.");
        if (room.PricePerNight < 0) return Fail("Price cannot be negative.");
        if (store.FindHotel(room.HotelId) is null) return Fail("Hotel does not exist.");

        var number = room.RoomNumber.Trim();
        if (store.Rooms.Any(r => r.HotelId == room.HotelId && r.RoomNumber == number))
            return Fail("Room number already exists in this hotel.");

        store.AddRoom(room.HotelId, number, room.RoomType.Trim(), room.Capacity, room.PricePerNight);
        return Ok();
    }

    public Task<AuthResult> RemoveRoomAsync(int roomId)
    {
        if (!IsAdmin)
            return NotAllowed();

        return store.RemoveRoom(roomId) ? Ok() : Fail("The room no longer exists.");
    }

    private bool IsAdmin => auth.CurrentUser?.IsAdmin == true;

    private static Task<AuthResult> Ok() => Task.FromResult(AuthResult.Ok());

    private static Task<AuthResult> Fail(string error) => Task.FromResult(AuthResult.Fail(error));

    private static Task<AuthResult> NotAllowed() => Fail("Only administrators can manage hotels and rooms.");
}
