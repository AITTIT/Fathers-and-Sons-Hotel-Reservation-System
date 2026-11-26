// HotelId is a foreign key indicating which hotel the room belongs to. The Hotel and Rooms navigation properties represent
// the same relationship in the C# models: one hotel can have multiple rooms, but a room belongs to one hotel.
using HotelReservation.Api.Features.Hotels;

namespace HotelReservation.Api.Features.Rooms;

public class Room
{
    public const int RoomNumberMaxLength = 20;

    public int Id { get; set; }

    public int HotelId { get; set; }

    public Hotel Hotel { get; set; } = null!;

    public string RoomNumber { get; set; } = string.Empty;

    public string RoomType { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public decimal PricePerNight { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}