namespace HotelReservationSystem.Features.Auth;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        viewModel.Completed += async (_, _) => await Shell.Current.GoToAsync("..");
    }

    private async void OnContinueAsGuest(object? sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync("..");
}
