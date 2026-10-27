using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api.Features.Common;

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public static class Paging
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    public static async Task<PagedResult<T>> ToPagedAsync<T>(this IQueryable<T> query, int? page, int? pageSize, CancellationToken ct)
    {
        var p = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((p - 1) * size).Take(size).ToListAsync(ct);
        return new PagedResult<T>(items, total, p, size);
    }
}
