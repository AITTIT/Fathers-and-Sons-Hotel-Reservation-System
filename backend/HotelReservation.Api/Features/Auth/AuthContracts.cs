using HotelReservation.Api.Features.Users;

namespace HotelReservation.Api.Features.Auth;

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Phone);

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, DateTime ExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc, UserResponse User);

public record RefreshRequest(string? RefreshToken);

public record LogoutRequest(string? RefreshToken);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Token, string NewPassword);
