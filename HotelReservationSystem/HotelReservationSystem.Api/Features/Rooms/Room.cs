//HotelId on vierasavain, joka osoittaa, mille hotellille huone kuuluu. Hotel- ja Rooms-navigaatio-ominaisuudet kuvaavat 
//samaa suhdetta C#-malleissa: yhdellä hotellilla voi olla useita huoneita, mutta huone kuuluu yhteen hotelliin.
using HotelReservationSystem.Api.Features.Hotels;

namespace HotelReservationSystem.Api.Features.Rooms;

public class Room
{
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