using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelReservationSystem.Features.Auth;

namespace HotelReservationSystem.Features.Admin;

/// <summary>One row of the user table.</summary>
public sealed class UserRowViewModel : ObservableObject
{
    private static readonly Color Green = Color.FromArgb("#2E8B57");
    private static readonly Color Grey = Color.FromArgb("#52646C");

    private readonly Func<UserRowViewModel, UserRole, Task> _changeRole;
    private string? _selectedRole;

    public UserRowViewModel(ManagedUser user, bool isSelf,
        Func<UserRowViewModel, UserRole, Task> changeRole, Func<UserRowViewModel, Task> toggleLock)
    {
        User = user;
        IsSelf = isSelf;
        _changeRole = changeRole;
        _selectedRole = user.Role.ToString();
        ToggleLockCommand = new AsyncRelayCommand(() => toggleLock(this));
    }

    public ManagedUser User { get; }

    /// <summary>The signed-in admin's own account, which is shown read-only.</summary>
    public bool IsSelf { get; }

    public bool CanManage => !IsSelf;

    public string Name => User.DisplayName;

    public string Email => User.Email;

    public IReadOnlyList<string> Roles { get; } = Enum.GetNames<UserRole>();

    public string? SelectedRole
    {
        get => _selectedRole;
        set
        {
            // The picker can push null while the row is being created or removed.
            if (value is null || !SetProperty(ref _selectedRole, value))
                return;

            _ = _changeRole(this, Enum.Parse<UserRole>(value));
        }
    }

    public string StatusText => User.IsLocked ? "○ Locked" : "● Active";

    public Color StatusColor => User.IsLocked ? Grey : Green;

    public string LockText => User.IsLocked ? "Unlock" : "Lock";

    public AsyncRelayCommand ToggleLockCommand { get; }
}
