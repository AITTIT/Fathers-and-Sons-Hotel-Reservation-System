using System.Collections.ObjectModel;
using HotelReservationSystem.Features.Rooms;
using HotelReservationSystem.Infrastructure;

namespace HotelReservationSystem.Features.RoomSearch;

public sealed record SearchCriteria(string City, DateTime CheckIn, DateTime CheckOut, int Guests);

public sealed class RoomSearchViewModel : ObservableObject
{
    private readonly IRoomService _rooms;
    private string _city = "Vaasa";
    private DateTime _checkIn = DateTime.Today;
    private DateTime _checkOut = DateTime.Today.AddDays(2);
    private int _guestsIndex = 1;
    private bool _loaded;

    public RoomSearchViewModel(IRoomService rooms)
    {
        _rooms = rooms;
        SearchCommand = new Command(OnSearch, () => !string.IsNullOrWhiteSpace(City));
    }

    /// <summary>Raised when the user submits the form. The results screen (C2) will subscribe to this later.</summary>
    public event EventHandler<SearchCriteria>? SearchSubmitted;

    public IReadOnlyList<string> GuestOptions { get; } =
        Enumerable.Range(1, 6).Select(n => n == 1 ? "1 guest" : $"{n} guests").ToList();

    public ObservableCollection<RoomCardViewModel> RecommendedRooms { get; } = [];

    public Command SearchCommand { get; }

    public string City
    {
        get => _city;
        set
        {
            if (SetProperty(ref _city, value))
                SearchCommand.ChangeCanExecute();
        }
    }

    public DateTime CheckIn
    {
        get => _checkIn;
        set
        {
            if (!SetProperty(ref _checkIn, value.Date))
                return;

            OnPropertyChanged(nameof(MinCheckOut));
            if (CheckOut <= _checkIn)
                CheckOut = _checkIn.AddDays(1);
        }
    }

    public DateTime CheckOut
    {
        get => _checkOut;
        set => SetProperty(ref _checkOut, value.Date);
    }

    public DateTime MinCheckIn => DateTime.Today;

    public DateTime MinCheckOut => CheckIn.AddDays(1);

    public int GuestsIndex
    {
        get => _guestsIndex;
        set => SetProperty(ref _guestsIndex, value < 0 ? 0 : value);
    }

    public async Task LoadAsync()
    {
        if (_loaded)
            return;

        _loaded = true;
        RecommendedRooms.Clear();
        foreach (var room in await _rooms.GetRecommendedAsync())
            RecommendedRooms.Add(new RoomCardViewModel(room));
    }

    private void OnSearch() =>
        SearchSubmitted?.Invoke(this, new SearchCriteria(City.Trim(), CheckIn, CheckOut, GuestsIndex + 1));
}
