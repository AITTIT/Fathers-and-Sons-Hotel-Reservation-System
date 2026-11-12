namespace HotelReservationSystem.Features.RoomSearch;

public partial class RoomSearchPage : ContentPage
{
    private readonly RoomSearchViewModel _viewModel;

    public RoomSearchPage(RoomSearchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _viewModel.SearchSubmitted += OnSearchSubmitted;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    // Responsive card grid: 3 columns on wide windows, fewer on narrow ones / phones.
    private void OnPageSizeChanged(object? sender, EventArgs e)
    {
        RoomsLayout.Span = Width switch
        {
            < 700 => 1,
            < 1100 => 2,
            _ => 3,
        };
    }

    private async void OnSearchSubmitted(object? sender, SearchCriteria criteria) =>
        await DisplayAlert(
            "Search rooms",
            $"{criteria.City}, {criteria.CheckIn:dd.MM.yyyy} – {criteria.CheckOut:dd.MM.yyyy}, " +
            $"{criteria.Guests} guest(s).\n\nThe results screen (C2) is not built yet.",
            "OK");
}
