namespace HotelReservationSystem.Features.Auth;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel, IAuthService auth)
    {
        InitializeComponent();
        BindingContext = viewModel;
        // Staff and admins land on their own start page; everyone else goes back to where they came from.
        viewModel.Completed += async (_, _) =>
            await Shell.Current.GoToAsync(auth.CurrentUser?.Role switch
            {
                UserRole.Admin => "//admin",
                UserRole.Staff => "//staff",
                _ => "..",
            });
    }

    private async void OnContinueAsGuest(object? sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync("..");
}
