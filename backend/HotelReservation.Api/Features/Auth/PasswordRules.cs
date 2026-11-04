namespace HotelReservation.Api.Features.Auth;

public static class PasswordRules
{
    private const int MinLength = 8;
    private const int WorkFactor = 11;

    public static string? Error(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            return $"Password must be at least {MinLength} characters.";
        // bcrypt only uses the first 72 bytes
        if (System.Text.Encoding.UTF8.GetByteCount(password) > 72)
            return "Password must be at most 72 bytes.";
        return null;
    }

    public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
}
