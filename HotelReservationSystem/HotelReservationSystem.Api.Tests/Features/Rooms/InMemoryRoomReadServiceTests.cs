using HotelReservationSystem.Api.Features.Rooms.Services;

namespace HotelReservationSystem.Api.Tests.Features.Rooms;

public class InMemoryRoomReadServiceTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsSeededRooms()
    {
        var service = new InMemoryRoomReadService();

        var rooms = await service.GetAllAsync();

        Assert.NotNull(rooms);
        Assert.Equal(3, rooms.Count);
        Assert.Contains(rooms, room => room.RoomNumber == "101");
        Assert.Contains(rooms, room => room.RoomType == "Suite");
    }
}
