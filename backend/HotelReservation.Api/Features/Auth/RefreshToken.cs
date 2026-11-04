using HotelReservation.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api.Features.Auth;

public class RefreshToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    // SHA-256 of the token; the raw token is only ever given to the client.
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    // Set when the token is exchanged. Presenting it again means it may have been stolen.
    public DateTime? UsedAtUtc { get; set; }

    // Set by logout, a password change, or detected reuse.
    public DateTime? RevokedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

public static class RefreshTokens
{
    public static Task RevokeAllAsync(AppDbContext db, int userId, DateTime now, CancellationToken ct) =>
        db.RefreshTokens
            .Where(t => t.UserId == userId && t.UsedAtUtc == null && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, now), ct);
}
