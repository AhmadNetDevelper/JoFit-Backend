namespace JoFit.Application.Common.Models;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Paging envelope expected by the front-ends: totals are exact, an empty result is page 1 of 1.</summary>
    public PageMeta Meta => new(
        Page,
        PageSize,
        TotalCount,
        Math.Max(1, TotalPages),
        HasNextPage,
        HasPreviousPage);
}

public sealed record PageMeta(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage);
