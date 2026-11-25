using HotelReservationSystem.Features.Auth;

namespace HotelReservationSystem.Features.Admin;

public sealed record ManagedUser(int Id, string Email, string FirstName, string LastName, UserRole Role, bool IsLocked)
{
    public string DisplayName => $"{FirstName} {LastName}".Trim();
}

public sealed record NewStaffAccount(string Email, string FirstName, string LastName, string Password);

/// <summary>
/// User administration for admins. The backend already covers listing (GET /api/users), role changes
/// (PUT /api/users/{id}) and password resets (POST /api/users/{id}/change-password).
/// It has no endpoints yet for creating staff accounts, locking or removing users.
/// </summary>
public interface IUserAdminService
{
    Task<IReadOnlyList<ManagedUser>> ListAsync();

    Task<AuthResult> CreateStaffAsync(NewStaffAccount account);

    Task<AuthResult> ChangeRoleAsync(int userId, UserRole role);

    Task<AuthResult> ResetPasswordAsync(int userId, string newPassword);

    Task<AuthResult> SetLockedAsync(int userId, bool isLocked);

    Task<AuthResult> RemoveAsync(int userId);
}
