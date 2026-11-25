namespace HotelReservationSystem.Features.Admin;

public partial class AdminHotelsPage : ContentPage
{
    private readonly AdminHotelsViewModel _viewModel;

    public AdminHotelsPage(AdminHotelsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.ActivateAsync();
    }

    private async void OnRemoveRoom(object? sender, EventArgs e)
    {
        if (sender is not BindableObject { BindingContext: RoomRow row })
            return;

        if (await DisplayAlert("Remove room",
                $"Remove room {row.Room.RoomNumber} from {row.Hotel.Name}? This can't be undone.", "Remove", "Keep"))
            await _viewModel.RemoveRoomAsync(row);
    }
}
