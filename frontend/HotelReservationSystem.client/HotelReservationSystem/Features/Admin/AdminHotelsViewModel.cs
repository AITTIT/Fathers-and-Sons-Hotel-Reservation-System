using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HotelReservationSystem.Features.Admin;

/// <summary>One room in a hotel's list.</summary>
public sealed record RoomRow(ManagedHotel Hotel, ManagedRoom Room)
{
    public string Text =>
        $"{Room.RoomNumber} · {Room.RoomType} · {Room.Capacity} guests · {Room.PricePerNight:0.##} €/night";
}

/// <summary>One hotel in the list, with its rooms.</summary>
public sealed record HotelRow(ManagedHotel Hotel, IReadOnlyList<ManagedRoom> Rooms)
{
    public string Name => Hotel.Name;

    public string Location => string.Join(", ",
        new[] { Hotel.AddressLine1, Hotel.PostalCode, Hotel.City, Hotel.Country }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

    public string RoomCount => Rooms.Count == 1 ? "1 room" : $"{Rooms.Count} rooms";

    public IReadOnlyList<RoomRow> RoomRows => Rooms.Select(r => new RoomRow(Hotel, r)).ToList();

    public bool HasNoRooms => Rooms.Count == 0;
}

/// <summary>A1 – hotel and room management.</summary>
public sealed class AdminHotelsViewModel : ObservableObject
{
    private readonly IHotelAdminService _service;

    private IReadOnlyList<HotelRow> _rows = [];
    private IReadOnlyList<ManagedHotel> _hotels = [];
    private string? _message;
    private bool _isError;

    private bool _isAddingHotel;
    private string _hotelName = "";
    private string _hotelAddress = "";
    private string _hotelCity = "";
    private string _hotelCountry = "Finland";
    private string _hotelPostalCode = "";
    private string? _hotelFormError;

    private bool _isAddingRoom;
    private ManagedHotel? _roomHotel;
    private string _roomNumber = "";
    private string _roomType = "";
    private string _roomCapacity = "2";
    private string _roomPrice = "";
    private string? _roomFormError;

    public AdminHotelsViewModel(IHotelAdminService service)
    {
        _service = service;
        ShowHotelFormCommand = new RelayCommand(() =>
        {
            CloseRoomForm();
            IsAddingHotel = true;
        });
        CancelHotelCommand = new RelayCommand(CloseHotelForm);
        CreateHotelCommand = new AsyncRelayCommand(CreateHotelAsync);
        ShowRoomFormCommand = new RelayCommand(() =>
        {
            CloseHotelForm();
            IsAddingRoom = true;
        });
        CancelRoomCommand = new RelayCommand(CloseRoomForm);
        CreateRoomCommand = new AsyncRelayCommand(CreateRoomAsync);
    }

    public RelayCommand ShowHotelFormCommand { get; }
    public RelayCommand CancelHotelCommand { get; }
    public AsyncRelayCommand CreateHotelCommand { get; }
    public RelayCommand ShowRoomFormCommand { get; }
    public RelayCommand CancelRoomCommand { get; }
    public AsyncRelayCommand CreateRoomCommand { get; }

    public IReadOnlyList<HotelRow> Rows
    {
        get => _rows;
        private set
        {
            if (SetProperty(ref _rows, value))
                OnPropertyChanged(nameof(HasNoRows));
        }
    }

    public bool HasNoRows => Rows.Count == 0;

    /// <summary>Choices for the room form's hotel picker.</summary>
    public IReadOnlyList<ManagedHotel> Hotels
    {
        get => _hotels;
        private set => SetProperty(ref _hotels, value);
    }

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

    public bool IsAddingHotel { get => _isAddingHotel; private set => SetProperty(ref _isAddingHotel, value); }

    public string HotelName { get => _hotelName; set => SetProperty(ref _hotelName, value); }

    public string HotelAddress { get => _hotelAddress; set => SetProperty(ref _hotelAddress, value); }

    public string HotelCity { get => _hotelCity; set => SetProperty(ref _hotelCity, value); }

    public string HotelCountry { get => _hotelCountry; set => SetProperty(ref _hotelCountry, value); }

    public string HotelPostalCode { get => _hotelPostalCode; set => SetProperty(ref _hotelPostalCode, value); }

    public string? HotelFormError
    {
        get => _hotelFormError;
        private set
        {
            if (SetProperty(ref _hotelFormError, value))
                OnPropertyChanged(nameof(HasHotelFormError));
        }
    }

    public bool HasHotelFormError => !string.IsNullOrEmpty(HotelFormError);

    public bool IsAddingRoom { get => _isAddingRoom; private set => SetProperty(ref _isAddingRoom, value); }

    public ManagedHotel? RoomHotel { get => _roomHotel; set => SetProperty(ref _roomHotel, value); }

    public string RoomNumber { get => _roomNumber; set => SetProperty(ref _roomNumber, value); }

    public string RoomType { get => _roomType; set => SetProperty(ref _roomType, value); }

    public string RoomCapacity { get => _roomCapacity; set => SetProperty(ref _roomCapacity, value); }

    public string RoomPrice { get => _roomPrice; set => SetProperty(ref _roomPrice, value); }

    public string? RoomFormError
    {
        get => _roomFormError;
        private set
        {
            if (SetProperty(ref _roomFormError, value))
                OnPropertyChanged(nameof(HasRoomFormError));
        }
    }

    public bool HasRoomFormError => !string.IsNullOrEmpty(RoomFormError);

    /// <summary>Called each time the page is shown: clears leftover input and reloads the data.</summary>
    public async Task ActivateAsync()
    {
        CloseHotelForm();
        CloseRoomForm();
        Message = null;
        await LoadAsync();
    }

    private async Task CreateHotelAsync()
    {
        HotelFormError = null;
        try
        {
            var name = HotelName.Trim();
            var result = await _service.CreateHotelAsync(
                new NewHotel(HotelName, HotelAddress, HotelCity, HotelCountry, HotelPostalCode));
            if (!result.Succeeded)
            {
                HotelFormError = result.Error;
                return;
            }

            CloseHotelForm();
            IsError = false;
            Message = $"Added the hotel {name}.";
            await LoadAsync();
        }
        catch (Exception)
        {
            HotelFormError = "Could not reach the server. Please try again.";
        }
    }

    public async Task RemoveRoomAsync(RoomRow row)
    {
        Message = null;
        try
        {
            var result = await _service.RemoveRoomAsync(row.Room.Id);
            IsError = !result.Succeeded;
            Message = result.Succeeded
                ? $"Removed room {row.Room.RoomNumber} from {row.Hotel.Name}."
                : result.Error;
            await LoadAsync();
        }
        catch (Exception)
        {
            IsError = true;
            Message = "Could not reach the server. Please try again.";
        }
    }

    private async Task CreateRoomAsync()
    {
        RoomFormError = null;

        if (RoomHotel is null)
        {
            RoomFormError = "Choose a hotel.";
            return;
        }

        if (!int.TryParse(RoomCapacity.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out var capacity))
        {
            RoomFormError = "Capacity must be a whole number.";
            return;
        }

        if (!TryParsePrice(RoomPrice, out var price))
        {
            RoomFormError = "Enter the price per night as a number, for example 89,50.";
            return;
        }

        try
        {
            var number = RoomNumber.Trim();
            var hotelName = RoomHotel.Name;
            var result = await _service.CreateRoomAsync(
                new NewRoom(RoomHotel.Id, RoomNumber, RoomType, capacity, price));
            if (!result.Succeeded)
            {
                RoomFormError = result.Error;
                return;
            }

            CloseRoomForm();
            IsError = false;
            Message = $"Added room {number} to {hotelName}.";
            await LoadAsync();
        }
        catch (Exception)
        {
            RoomFormError = "Could not reach the server. Please try again.";
        }
    }

    // Accepts both "89,50" and "89.50" regardless of the device's culture.
    private static bool TryParsePrice(string text, out decimal price)
    {
        text = text.Trim();
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out price)
               || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out price);
    }

    private async Task LoadAsync()
    {
        try
        {
            await Task.Yield();
            var hotels = await _service.ListHotelsAsync();
            var rooms = await _service.ListRoomsAsync();

            Hotels = hotels;
            Rows = hotels
                .Select(h => new HotelRow(h, rooms.Where(r => r.HotelId == h.Id).OrderBy(r => r.RoomNumber).ToList()))
                .ToList();
        }
        catch (Exception)
        {
            IsError = true;
            Message = "Could not reach the server. Please try again.";
        }
    }

    private void CloseHotelForm()
    {
        IsAddingHotel = false;
        HotelName = HotelAddress = HotelCity = HotelPostalCode = "";
        HotelCountry = "Finland";
        HotelFormError = null;
    }

    private void CloseRoomForm()
    {
        IsAddingRoom = false;
        RoomHotel = null;
        RoomNumber = RoomType = RoomPrice = "";
        RoomCapacity = "2";
        RoomFormError = null;
    }
}
