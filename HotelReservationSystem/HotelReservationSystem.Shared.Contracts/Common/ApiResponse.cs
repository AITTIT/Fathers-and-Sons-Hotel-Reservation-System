namespace HotelReservationSystem.Shared.Contracts.Common;

public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Error,
    string? ErrorCode)
{
    public static ApiResponse<T> Ok(T data) => new(true, data, null, null);

    public static ApiResponse<T> Fail(string error, string? errorCode = null) =>
        new(false, default, error, errorCode);
}
