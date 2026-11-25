namespace HotelReservationSystem.Features.Auth;

/// <summary>
/// In-memory stand-in until the API client exists.
/// Demo logins: guest@example.com, staff@example.com, admin@example.com (password: Password1)
/// </summary>
public sealed class MockAuthService(MockUserStore store) : IAuthService
{
    public SignedInUser? CurrentUser { get; private set; }

    public event EventHandler? CurrentUserChanged;

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        await Task.Delay(400);

        var account = store.Find(email);
        if (account is null || account.Password != password)
            return AuthResult.Fail("Incorrect email or password.");

        if (account.IsLocked)
            return AuthResult.Fail("This account is locked. Contact an administrator.");

        SetUser(account.ToSignedInUser());
        return AuthResult.Ok();
    }

    public async Task<AuthResult> RegisterAsync(RegisterDetails d)
    {
        await Task.Delay(400);

        if (store.Find(d.Email) is not null)
            return AuthResult.Fail("An account with this email already exists.");

        var account = store.Add(d.Email, d.Password, d.FirstName, d.LastName);
        SetUser(account.ToSignedInUser());
        return AuthResult.Ok();
    }

    public Task LogoutAsync()
    {
        SetUser(null);
        return Task.CompletedTask;
    }

    private void SetUser(SignedInUser? user)
    {
        CurrentUser = user;
        CurrentUserChanged?.Invoke(this, EventArgs.Empty);
    }
}
