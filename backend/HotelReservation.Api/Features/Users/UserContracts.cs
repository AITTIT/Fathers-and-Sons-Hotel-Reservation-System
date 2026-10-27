namespace HotelReservation.Api.Features.Users;

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Phone);

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, DateTime ExpiresAtUtc, UserResponse User);

public record UserResponse(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    string Role,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public static UserResponse From(User u) => new(
        u.Id, u.Email, u.FirstName, u.LastName, u.Phone, u.Role, u.CreatedAtUtc, u.UpdatedAtUtc);
}
