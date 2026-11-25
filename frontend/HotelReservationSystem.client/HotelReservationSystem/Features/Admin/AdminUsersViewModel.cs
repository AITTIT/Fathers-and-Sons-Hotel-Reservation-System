using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelReservationSystem.Features.Auth;

namespace HotelReservationSystem.Features.Admin;

/// <summary>A role filter above the table.</summary>
public sealed record RoleTab(string Text, bool IsSelected, IRelayCommand SelectCommand);

/// <summary>A1 – user management.</summary>
public sealed class AdminUsersViewModel : ObservableObject
{
    private readonly IUserAdminService _users;
    private readonly IAuthService _auth;

    private IReadOnlyList<ManagedUser> _allUsers = [];
    private IReadOnlyList<UserRowViewModel> _rows = [];
    private IReadOnlyList<RoleTab> _tabs = [];
    private UserRole? _roleFilter; // null shows all users
    private string _searchText = "";
    private string? _message;
    private bool _isError;
    private bool _isCreating;
    private string _newFirstName = "";
    private string _newLastName = "";
    private string _newEmail = "";
    private string _newPassword = "";
    private string? _formError;

    public AdminUsersViewModel(IUserAdminService users, IAuthService auth)
    {
        _users = users;
        _auth = auth;
        ShowCreateFormCommand = new RelayCommand(() => IsCreating = true);
        CancelCreateCommand = new RelayCommand(CloseCreateForm);
        CreateStaffCommand = new AsyncRelayCommand(CreateStaffAsync);
    }

    public RelayCommand ShowCreateFormCommand { get; }

    public RelayCommand CancelCreateCommand { get; }

    public AsyncRelayCommand CreateStaffCommand { get; }

    public IReadOnlyList<UserRowViewModel> Rows
    {
        get => _rows;
        private set
        {
            if (SetProperty(ref _rows, value))
                OnPropertyChanged(nameof(HasNoRows));
        }
    }

    public bool HasNoRows => Rows.Count == 0;

    public IReadOnlyList<RoleTab> Tabs
    {
        get => _tabs;
        private set => SetProperty(ref _tabs, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value ?? ""))
                ApplyFilter();
        }
    }

    /// <summary>Outcome of the latest action.</summary>
    public string? Message
    {
        get => _message;
        private set
        {
            if (SetProperty(ref _message, value))
                OnPropertyChanged(nameof(HasMessage));
        }
    }

    public bool HasMessage => !string.IsNullOrEmpty(Message);

    public bool IsError { get => _isError; private set => SetProperty(ref _isError, value); }

    public bool IsCreating { get => _isCreating; private set => SetProperty(ref _isCreating, value); }

    public string NewFirstName { get => _newFirstName; set => SetProperty(ref _newFirstName, value); }

    public string NewLastName { get => _newLastName; set => SetProperty(ref _newLastName, value); }

    public string NewEmail { get => _newEmail; set => SetProperty(ref _newEmail, value); }

    public string NewPassword { get => _newPassword; set => SetProperty(ref _newPassword, value); }

    public string? FormError
    {
        get => _formError;
        private set
        {
            if (SetProperty(ref _formError, value))
                OnPropertyChanged(nameof(HasFormError));
        }
    }

    public bool HasFormError => !string.IsNullOrEmpty(FormError);

    /// <summary>Called each time the page is shown: clears leftover input and reloads the users.</summary>
    public async Task ActivateAsync()
    {
        CloseCreateForm();
        Message = null;
        _roleFilter = null;
        SearchText = "";
        await LoadAsync();
    }

    public Task ResetPasswordAsync(UserRowViewModel row, string newPassword) =>
        RunAsync(() => _users.ResetPasswordAsync(row.User.Id, newPassword), $"Password reset for {row.Name}.");

    public Task RemoveAsync(UserRowViewModel row) =>
        RunAsync(() => _users.RemoveAsync(row.User.Id), $"Removed {row.Name}.");

    private Task ChangeRoleAsync(UserRowViewModel row, UserRole role) =>
        RunAsync(() => _users.ChangeRoleAsync(row.User.Id, role), $"{row.Name} now has the {role} role.");

    private Task ToggleLockAsync(UserRowViewModel row) =>
        RunAsync(() => _users.SetLockedAsync(row.User.Id, !row.User.IsLocked),
            $"{(row.User.IsLocked ? "Unlocked" : "Locked")} {row.Name}.");

    private async Task CreateStaffAsync()
    {
        FormError = null;
        try
        {
            var name = $"{NewFirstName.Trim()} {NewLastName.Trim()}";
            var result = await _users.CreateStaffAsync(
                new NewStaffAccount(NewEmail, NewFirstName, NewLastName, NewPassword));
            if (!result.Succeeded)
            {
                FormError = result.Error;
                return;
            }

            CloseCreateForm();
            IsError = false;
            Message = $"Created a staff account for {name}.";
            await LoadAsync();
        }
        catch (Exception)
        {
            FormError = "Could not reach the server. Please try again.";
        }
    }

    private async Task RunAsync(Func<Task<AuthResult>> action, string successMessage)
    {
        Message = null;
        try
        {
            var result = await action();
            IsError = !result.Succeeded;
            Message = result.Succeeded ? successMessage : result.Error;
            // Reload even after a failure so that the row shows the saved values again.
            await LoadAsync();
        }
        catch (Exception)
        {
            IsError = true;
            Message = "Could not reach the server. Please try again.";
        }
    }

    private async Task LoadAsync()
    {
        // Yield first: a reload replaces the rows, possibly while one of their controls is still raising an event.
        await Task.Yield();
        _allUsers = await _users.ListAsync();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var term = SearchText.Trim();

        Rows = _allUsers
            .Where(u => _roleFilter is null || u.Role == _roleFilter)
            .Where(u => u.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || u.Email.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Select(u => new UserRowViewModel(u, IsSignedInUser(u), ChangeRoleAsync, ToggleLockAsync))
            .ToList();

        Tabs =
        [
            Tab("All", null),
            Tab("Customers", UserRole.Customer),
            Tab("Staff", UserRole.Staff),
            Tab("Admins", UserRole.Admin),
        ];
    }

    private RoleTab Tab(string label, UserRole? role) => new(
        $"{label} {_allUsers.Count(u => role is null || u.Role == role)}",
        _roleFilter == role,
        new RelayCommand(() =>
        {
            _roleFilter = role;
            ApplyFilter();
        }));

    private bool IsSignedInUser(ManagedUser user) =>
        string.Equals(user.Email, _auth.CurrentUser?.Email, StringComparison.OrdinalIgnoreCase);

    private void CloseCreateForm()
    {
        IsCreating = false;
        NewFirstName = NewLastName = NewEmail = NewPassword = "";
        FormError = null;
    }
}
