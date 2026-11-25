using System.Net.Mail;
using System.Text;
using HotelReservationSystem.Features.Auth;

namespace HotelReservationSystem.Features.Admin;

/// <summary>In-memory stand-in until the API client exists.</summary>
public sealed class MockUserAdminService(MockUserStore store, IAuthService auth) : IUserAdminService
{
    public Task<IReadOnlyList<ManagedUser>> ListAsync()
    {
        IReadOnlyList<ManagedUser> users = IsAdmin
            ? store.Accounts
                .Select(a => new ManagedUser(a.Id, a.Email, a.FirstName, a.LastName, a.Role, a.IsLocked))
                .ToList()
            : [];
        return Task.FromResult(users);
    }

    public Task<AuthResult> CreateStaffAsync(NewStaffAccount account)
    {
        if (!IsAdmin)
            return NotAllowed();

        if (string.IsNullOrWhiteSpace(account.FirstName) || string.IsNullOrWhiteSpace(account.LastName))
            return Fail("Enter a first and last name.");

        var email = account.Email.Trim();
        if (!MailAddress.TryCreate(email, out var address) || address.Address != email)
            return Fail("Enter a valid email address.");

        if (PasswordError(account.Password) is { } passwordError)
            return Fail(passwordError);

        if (store.Find(email) is not null)
            return Fail("An account with this email already exists.");

        store.Add(email, account.Password, account.FirstName, account.LastName, UserRole.Staff);
        return Ok();
    }

    public Task<AuthResult> ChangeRoleAsync(int userId, UserRole role) =>
        UpdateOtherUser(userId, a => a.Role = role);

    public Task<AuthResult> ResetPasswordAsync(int userId, string newPassword) =>
        PasswordError(newPassword) is { } error
            ? Fail(error)
            : UpdateOtherUser(userId, a => a.Password = newPassword);

    public Task<AuthResult> SetLockedAsync(int userId, bool isLocked) =>
        UpdateOtherUser(userId, a => a.IsLocked = isLocked);

    public Task<AuthResult> RemoveAsync(int userId) =>
        UpdateOtherUser(userId, store.Remove);

    private bool IsAdmin => auth.CurrentUser?.IsAdmin == true;

    // Admins manage other accounts only, so nobody can lock themselves out of administration.
    private Task<AuthResult> UpdateOtherUser(int userId, Action<MockAccount> change)
    {
        if (!IsAdmin)
            return NotAllowed();

        var account = store.Accounts.FirstOrDefault(a => a.Id == userId);
        if (account is null)
            return Fail("The user no longer exists.");

        if (account == store.Find(auth.CurrentUser!.Email))
            return Fail("You can't change your own account here.");

        change(account);
        return Ok();
    }

    // Same limits as the backend's PasswordRules (bcrypt only uses the first 72 bytes).
    private static string? PasswordError(string password)
    {
        if (password.Length < 8)
            return "Password must be at least 8 characters.";

        return Encoding.UTF8.GetByteCount(password) > 72 ? "Password must be at most 72 bytes." : null;
    }

    private static Task<AuthResult> Ok() => Task.FromResult(AuthResult.Ok());

    private static Task<AuthResult> Fail(string error) => Task.FromResult(AuthResult.Fail(error));

    private static Task<AuthResult> NotAllowed() => Fail("Only administrators can manage users.");
}
