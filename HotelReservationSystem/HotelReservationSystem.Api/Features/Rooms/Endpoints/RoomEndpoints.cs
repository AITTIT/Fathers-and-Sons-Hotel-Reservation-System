using HotelReservationSystem.Api.Features.Rooms.Abstractions;
using HotelReservationSystem.Shared.Contracts.Common;
using HotelReservationSystem.Shared.Contracts.Rooms;

namespace HotelReservationSystem.Api.Features.Rooms.Endpoints;

public static class RoomEndpoints
{
    public static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rooms").WithTags("Rooms");

        group.MapGet("/", async (IRoomReadService roomReadService, CancellationToken cancellationToken) =>
        {
            var rooms = await roomReadService.GetAllAsync(cancellationToken);
            var response = ApiResponse<GetRoomsResponse>.Ok(new GetRoomsResponse(rooms));
            return Results.Ok(response);
        })
        .WithName("GetRooms")
        .Produces<ApiResponse<GetRoomsResponse>>(StatusCodes.Status200OK);

        return app;
    }
}
