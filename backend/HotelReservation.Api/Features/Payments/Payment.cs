namespace HotelReservation.Api.Features.Payments;

public class Payment
{
    public int Id { get; set; }

    public int ReservationId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string? ProviderTransactionId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "EUR";

    public string Status { get; set; } = "Pending";

    public DateTime? PaidAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
