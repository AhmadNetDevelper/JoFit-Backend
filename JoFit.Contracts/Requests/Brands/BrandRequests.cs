using JoFit.Contracts.Requests.Common;

namespace JoFit.Contracts.Requests.Brands;

/// <summary>Create and update body of a brand.</summary>
public sealed record BrandDraftRequest
{
    public string Slug { get; init; } = string.Empty;
    public LocalizedTextRequest Name { get; init; } = new();
    public MediaImageRequest? Logo { get; init; }
}

public sealed record GetBrandsPagedRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
    public string? Locale { get; init; }
}
