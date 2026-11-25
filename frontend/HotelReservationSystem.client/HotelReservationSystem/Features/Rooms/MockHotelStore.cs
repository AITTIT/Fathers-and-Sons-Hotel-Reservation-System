namespace HotelReservationSystem.Features.Rooms;

public sealed class MockHotel
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string AddressLine1 { get; init; }
    public required string City { get; init; }
    public required string Country { get; init; }
    public string? PostalCode { get; init; }
}

public sealed class MockRoom
{
    public required int Id { get; init; }
    public required int HotelId { get; init; }
    public required string RoomNumber { get; init; }
    public required string RoomType { get; init; }
    public required int Capacity { get; init; }
    public required decimal PricePerNight { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// In-memory hotels and rooms shared by the mock room search and hotel administration services,
/// so that what an admin adds shows up in the room search. Reset when the app restarts.
/// </summary>
public sealed class MockHotelStore
{
    private readonly List<MockHotel> _hotels = [];
    private readonly List<MockRoom> _rooms = [];
    private int _nextHotelId = 1;
    private int _nextRoomId = 1;

    public MockHotelStore()
    {
        var rantakatu = AddHotel("Hotel Rantakatu", "Rantakatu 1", "Vaasa", "Finland", "65100");
        var keskusta = AddHotel("Hotel Keskusta", "Kauppapuistikko 5", "Vaasa", "Finland", "65100");
        var satama = AddHotel("Hotel Satama", "Satamakatu 3", "Helsinki", "Finland", "00160");

        AddRoom(rantakatu.Id, "101", "Double", 2, 89m);
        AddRoom(keskusta.Id, "201", "Suite", 4, 159m);
        AddRoom(rantakatu.Id, "102", "Single", 1, 59m);
        AddRoom(satama.Id, "301", "Double", 2, 129m);
    }

    public IReadOnlyList<MockHotel> Hotels => _hotels;

    public IReadOnlyList<MockRoom> Rooms => _rooms;

    public MockHotel? FindHotel(int id) => _hotels.Find(h => h.Id == id);

    public MockHotel AddHotel(string name, string address, string city, string country, string? postalCode)
    {
        var hotel = new MockHotel
        {
            Id = _nextHotelId++,
            Name = name,
            AddressLine1 = address,
            City = city,
            Country = country,
            PostalCode = postalCode,
        };
        _hotels.Add(hotel);
        return hotel;
    }

    public bool RemoveRoom(int id) => _rooms.RemoveAll(r => r.Id == id) > 0;

    public MockRoom AddRoom(int hotelId, string roomNumber, string roomType, int capacity, decimal price)
    {
        var room = new MockRoom
        {
            Id = _nextRoomId++,
            HotelId = hotelId,
            RoomNumber = roomNumber,
            RoomType = roomType,
            Capacity = capacity,
            PricePerNight = price,
        };
        _rooms.Add(room);
        return room;
    }
}
