using HotelReservationSystem.Shared.Contracts.Common;

namespace HotelReservationSystem.Api.Tests.Common;

public class ApiResponseTests
{
    [Fact]
    public void Ok_SetsSuccessAndData()
    {
        var response = ApiResponse<string>.Ok("value");

        Assert.True(response.Success);
        Assert.Equal("value", response.Data);
        Assert.Null(response.Error);
        Assert.Null(response.ErrorCode);
    }

    [Fact]
    public void Fail_SetsErrorAndCode()
    {
        var response = ApiResponse<string>.Fail("bad request", "ROOMS_001");

        Assert.False(response.Success);
        Assert.Null(response.Data);
        Assert.Equal("bad request", response.Error);
        Assert.Equal("ROOMS_001", response.ErrorCode);
    }
}
