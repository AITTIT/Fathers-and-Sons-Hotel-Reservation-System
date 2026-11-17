using System.Globalization;
using HotelReservationSystem.Features.Rooms;

namespace HotelReservationSystem.Features.RoomSearch;

/// <summary>Display-ready wrapper around a <see cref="RoomSummary"/> for the room cards.</summary>
public sealed class RoomCardViewModel
{
    private readonly RoomSummary _room;

    public RoomCardViewModel(RoomSummary room) => _room = room;

    public int Id => _room.Id;

    public string Title => _room.RoomType == "Suite" ? _room.RoomType : $"{_room.RoomType} room";

    public string Meta =>
        $"{_room.HotelName} · {_room.City} · {_room.Capacity} {(_room.Capacity == 1 ? "guest" : "guests")}";

    public string Price => _room.PricePerNight.ToString("0.##", CultureInfo.InvariantCulture) + " €";

    // Placeholder tint until real room photos exist.
    public Color PhotoColor => _room.RoomType switch
    {
        "Double" => ResourceColor("PhotoTeal"),
        "Suite" => ResourceColor("PhotoSand"),
        "Single" => ResourceColor("PhotoLavender"),
        _ => ResourceColor("Secondary"),
    };

    private static Color ResourceColor(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : Colors.LightGray;
}
