using System.Net.Mail;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HotelReservationSystem.Features.Auth;

public sealed class LoginViewModel : ObservableObject
{
    private const int MinPasswordLength = 8;

    private readonly IAuthService _auth;
    private bool _isRegisterMode;
    private bool _isBusy;
    private string _email = "";
    private string _password = "";
    private string _firstName = "";
    private string _lastName = "";
    private string _phone = "";
    private string? _error;

    public LoginViewModel(IAuthService auth)
    {
        _auth = auth;
        SubmitCommand = new AsyncRelayCommand(SubmitAsync, () => !IsBusy);
        ToggleModeCommand = new RelayCommand(() => IsRegisterMode = !IsRegisterMode);
    }

    /// <summary>Raised after a successful sign in / registration so the page can close itself.</summary>
    public event EventHandler? Completed;

    public AsyncRelayCommand SubmitCommand { get; }

    public RelayCommand ToggleModeCommand { get; }

    public bool IsRegisterMode
    {
        get => _isRegisterMode;
        set
        {
            if (!SetProperty(ref _isRegisterMode, value))
                return;

            Error = null;
            OnPropertyChanged(nameof(Heading));
            OnPropertyChanged(nameof(Subheading));
            OnPropertyChanged(nameof(SubmitText));
            OnPropertyChanged(nameof(ToggleHint));
            OnPropertyChanged(nameof(ToggleText));
        }
    }

    public string Heading => IsRegisterMode ? "Create your account" : "Welcome back";

    public string Subheading => IsRegisterMode
        ? "Register to book rooms and manage your reservations."
        : "Sign in to view and manage your bookings.";

    public string SubmitText => IsBusy ? "Please wait…" : IsRegisterMode ? "Create account" : "Sign in";

    public string ToggleHint => IsRegisterMode ? "Already have an account?" : "New here?";

    public string ToggleText => IsRegisterMode ? "Sign in" : "Create an account";

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
                return;

            OnPropertyChanged(nameof(SubmitText));
            SubmitCommand.NotifyCanExecuteChanged();
        }
    }

    public string Email { get => _email; set => SetProperty(ref _email, value); }

    public string Password { get => _password; set => SetProperty(ref _password, value); }

    public string FirstName { get => _firstName; set => SetProperty(ref _firstName, value); }

    public string LastName { get => _lastName; set => SetProperty(ref _lastName, value); }

    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }

    public string? Error
    {
        get => _error;
        private set
        {
            if (SetProperty(ref _error, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(Error);

    private async Task SubmitAsync()
    {
        Error = Validate();
        if (Error is not null)
            return;

        IsBusy = true;
        try
        {
            var result = IsRegisterMode
                ? await _auth.RegisterAsync(new RegisterDetails(
                    Email.Trim(), Password, FirstName, LastName,
                    string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim()))
                : await _auth.LoginAsync(Email, Password);

            if (result.Succeeded)
            {
                Password = "";
                Completed?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Error = result.Error ?? "Something went wrong. Please try again.";
            }
        }
        catch (Exception)
        {
            Error = "Could not reach the server. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string? Validate()
    {
        if (!IsValidEmail(Email))
            return "Enter a valid email address.";

        if (string.IsNullOrEmpty(Password))
            return "Enter your password.";

        if (!IsRegisterMode)
            return null;

        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
            return "Enter your first and last name.";

        if (Password.Length < MinPasswordLength)
            return $"Password must be at least {MinPasswordLength} characters.";

        return null;
    }

    private static bool IsValidEmail(string value) =>
        MailAddress.TryCreate(value?.Trim(), out var address) && address.Address == value!.Trim();
}
