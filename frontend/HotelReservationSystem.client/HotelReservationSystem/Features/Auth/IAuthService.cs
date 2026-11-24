namespace HotelReservationSystem.Features.Auth;

public enum UserRole { Customer, Staff, Admin }

public sealed record SignedInUser(string Email, string FirstName, string LastName, UserRole Role = UserRole.Customer)
{
    public bool IsStaff => Role == UserRole.Staff;

    public string DisplayName => $"{FirstName} {LastName}".Trim();
}

public sealed record RegisterDetails(string Email, string Password, string FirstName, string LastName, string? Phone);

/// <summary>Success or a user-facing error message.</summary>
public sealed record AuthResult(bool Succeeded, string? Error = null)
{
    public static AuthResult Ok() => new(true);
    public static AuthResult Fail(string error) => new(false, error);
}

/// <summary>
/// Mirrors the backend's POST /api/auth/login and /api/auth/register.
/// Swap MockAuthService for an HttpClient-backed implementation when the client talks to the API.
/// </summary>
public interface IAuthService
{
    SignedInUser? CurrentUser { get; }

    event EventHandler? CurrentUserChanged;

    Task<AuthResult> LoginAsync(string email, string password);

    Task<AuthResult> RegisterAsync(RegisterDetails details);

    Task LogoutAsync();
}
