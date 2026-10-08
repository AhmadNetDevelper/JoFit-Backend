using JoFit.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Common.Extensions;

public static class PagingExtensions
{
    public const int DefaultPageSize = 20;
    public const int AdminMaxPageSize = 100;
    public const int PublicMaxPageSize = 200;

    /// <summary>
    /// Pages a query. pageSize below 1 becomes 1, above maxPageSize is capped, and a page past the
    /// end answers the last page rather than an empty one.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken ct,
        int maxPageSize = AdminMaxPageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, maxPageSize);
        var total = await query.CountAsync(ct);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<T>(items, page, pageSize, total);
    }
}
