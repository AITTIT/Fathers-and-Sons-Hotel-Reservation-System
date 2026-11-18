namespace HotelReservationSystem.Features.Auth;

/// <summary>In-memory stand-in until the API client exists. Demo login: guest@example.com / Password1</summary>
public sealed class MockAuthService : IAuthService
{
    private readonly Dictionary<string, (string Password, SignedInUser User)> _users =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["guest@example.com"] = ("Password1", new SignedInUser("guest@example.com", "Demo", "Guest")),
        };

    public SignedInUser? CurrentUser { get; private set; }

    public event EventHandler? CurrentUserChanged;

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        await Task.Delay(400);

        if (!_users.TryGetValue(email.Trim(), out var entry) || entry.Password != password)
            return AuthResult.Fail("Incorrect email or password.");

        SetUser(entry.User);
        return AuthResult.Ok();
    }

    public async Task<AuthResult> RegisterAsync(RegisterDetails d)
    {
        await Task.Delay(400);

        var email = d.Email.Trim();
        if (_users.ContainsKey(email))
            return AuthResult.Fail("An account with this email already exists.");

        var user = new SignedInUser(email, d.FirstName.Trim(), d.LastName.Trim());
        _users[email] = (d.Password, user);
        SetUser(user);
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
