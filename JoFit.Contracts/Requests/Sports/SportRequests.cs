using JoFit.Contracts.Requests.Common;

namespace JoFit.Contracts.Requests.Sports;

/// <summary>Create and update body of a sport.</summary>
public sealed record SportDraftRequest
{
    public string? Slug { get; init; }
    public LocalizedTextRequest Name { get; init; } = new();
    public LocalizedTextRequest? Description { get; init; }
    public MediaImageRequest? Image { get; init; }
    public bool IsFeatured { get; init; }
}

public sealed record GetSportsPagedRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
    public string? Locale { get; init; }
}
