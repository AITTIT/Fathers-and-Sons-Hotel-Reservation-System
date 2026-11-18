namespace HotelReservationSystem.Features.Staff;

public partial class StaffDashboardPage : ContentPage
{
    public StaffDashboardPage(StaffDashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnNewReservation(object? sender, EventArgs e) =>
        await DisplayAlert("New reservation", "The edit reservation screen (S3) is not built yet.", "OK");

    private async void OnOpenReservations(object? sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync("//staffreservations");
}
