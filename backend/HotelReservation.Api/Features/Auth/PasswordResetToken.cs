namespace HotelReservation.Api.Features.Users;

public class PasswordResetToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    // SHA-256 of the token; the raw token is only ever given to the user.
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? UsedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
