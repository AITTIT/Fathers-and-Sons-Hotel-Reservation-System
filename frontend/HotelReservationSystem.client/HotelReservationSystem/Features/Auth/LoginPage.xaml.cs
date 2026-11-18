namespace HotelReservationSystem.Features.Auth;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel, IAuthService auth)
    {
        InitializeComponent();
        BindingContext = viewModel;
        // Staff land on their dashboard; everyone else goes back to where they came from.
        viewModel.Completed += async (_, _) =>
            await Shell.Current.GoToAsync(auth.CurrentUser?.IsStaff == true ? "//staff" : "..");
    }

    private async void OnContinueAsGuest(object? sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync("..");
}
