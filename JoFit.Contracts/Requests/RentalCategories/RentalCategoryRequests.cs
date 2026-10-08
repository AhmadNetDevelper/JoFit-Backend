using JoFit.Contracts.Requests.Common;

namespace JoFit.Contracts.Requests.RentalCategories;

/// <summary>Create and update body of a rental category.</summary>
public sealed record RentalCategoryDraftRequest
{
    public string? Slug { get; init; }
    public LocalizedTextRequest Name { get; init; } = new();
    public LocalizedTextRequest? Description { get; init; }
    public MediaImageRequest? Image { get; init; }
    public long? ParentId { get; init; }
}

public sealed record GetRentalCategoriesPagedRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
    public string? Locale { get; init; }
}
