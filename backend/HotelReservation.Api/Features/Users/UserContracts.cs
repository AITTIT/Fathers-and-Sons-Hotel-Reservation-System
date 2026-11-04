namespace HotelReservation.Api.Features.Users;

// Role is optional; only admins may change it.
public record UpdateUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    string? Role);

// CurrentPassword is required when changing your own password; admins changing someone else's may omit it.
public record ChangePasswordRequest(string? CurrentPassword, string NewPassword);

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
