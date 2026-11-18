using HotelReservationSystem.Features.Auth;
using HotelReservationSystem.Infrastructure;

namespace HotelReservationSystem.Features.Staff;

public sealed record StatTile(string Label, string Value, string Detail, bool IsAlert = false);

public sealed record HotelAvailability(string Name, int Free, int Total)
{
    public override string ToString() => $"{Name} · {Free} free";
}

public sealed record ArrivalRow(string Booking, string Guest, string Room, string Status, Color StatusColor);

/// <summary>S1 – staff dashboard. Figures are mock data until the reservations API is wired in.</summary>
public sealed class StaffDashboardViewModel : ObservableObject
{
    private HotelAvailability _selectedHotel;
    private IReadOnlyList<StatTile> _tiles = [];

    private static readonly Color Green = Color.FromArgb("#2E8B57");
    private static readonly Color Amber = Color.FromArgb("#C98412");

    public StaffDashboardViewModel(IAuthService auth)
    {
        var now = DateTime.Now;
        var name = auth.CurrentUser?.FirstName ?? "";
        DateText = now.ToString("dddd dd.MM.yyyy");
        var part = now.Hour < 12 ? "Good morning" : now.Hour < 18 ? "Good afternoon" : "Good evening";
        Greeting = string.IsNullOrEmpty(name) ? part : $"{part}, {name}";

        Hotels =
        [
            new("Helsinki", 14, 40),
            new("Tampere", 9, 28),
            new("Turku", 6, 22),
            new("Oulu", 11, 30),
        ];
        _selectedHotel = Hotels[0];
        UpdateTiles();

        Arrivals =
        [
            new("#10228", "A. Mäkinen", "108 Sgl", "● In", Green),
            new("#10231", "S. Korhonen", "214 Dbl", "● Confirmed", Green),
            new("#10233", "M. Nieminen", "305 Dbl", "◐ Pending", Amber),
        ];
    }

    public string DateText { get; }

    public string Greeting { get; }

    public IReadOnlyList<HotelAvailability> Hotels { get; }

    public HotelAvailability SelectedHotel
    {
        get => _selectedHotel;
        set
        {
            if (value is null || !SetProperty(ref _selectedHotel, value)) return;
            UpdateTiles();
        }
    }

    public IReadOnlyList<StatTile> Tiles
    {
        get => _tiles;
        private set => SetProperty(ref _tiles, value);
    }

    private void UpdateTiles() =>
        Tiles = [new("Free rooms now", _selectedHotel.Free.ToString(), $"of {_selectedHotel.Total} in {_selectedHotel.Name}")];

    public IReadOnlyList<ArrivalRow> Arrivals { get; }
}
