namespace HotelReservation.Api.Features.Reservations;

public class Reservation
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int HotelId { get; set; }

    public int RoomId { get; set; }

    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }

    public int GuestCount { get; set; }

    public string Status { get; set; } = "Pending";

    public decimal TotalPrice { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
