namespace HotelReservationSystem.Features.Auth;

public sealed class MockAccount
{
    public required int Id { get; init; }
    public required string Email { get; init; }
    public required string Password { get; set; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public UserRole Role { get; set; } = UserRole.Customer;
    public bool IsLocked { get; set; }

    public SignedInUser ToSignedInUser() => new(Email, FirstName, LastName, Role);
}

/// <summary>
/// In-memory accounts shared by the mock sign-in and user administration services,
/// so that an admin's changes affect who can sign in. Reset when the app restarts.
/// </summary>
public sealed class MockUserStore
{
    private readonly List<MockAccount> _accounts = [];
    private int _nextId = 1;

    public MockUserStore()
    {
        Add("guest@example.com", "Password1", "Demo", "Guest");
        Add("staff@example.com", "Password1", "Juha", "Laine", UserRole.Staff);
        Add("admin@example.com", "Password1", "IT", "Support", UserRole.Admin);
        Add("maija.virtanen@example.com", "Password1", "Maija", "Virtanen");
        Add("mikko.nieminen@example.com", "Password1", "Mikko", "Nieminen").IsLocked = true;
        Add("sanna.korhonen@example.com", "Password1", "Sanna", "Korhonen");
    }

    public IReadOnlyList<MockAccount> Accounts => _accounts;

    public MockAccount? Find(string email) =>
        _accounts.Find(a => string.Equals(a.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));

    public MockAccount Add(string email, string password, string firstName, string lastName, UserRole role = UserRole.Customer)
    {
        var account = new MockAccount
        {
            Id = _nextId++,
            Email = email.Trim().ToLowerInvariant(),
            Password = password,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Role = role,
        };
        _accounts.Add(account);
        return account;
    }

    public void Remove(MockAccount account) => _accounts.Remove(account);
}
