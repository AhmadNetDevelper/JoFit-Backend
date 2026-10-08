using JoFit.Contracts.Requests.Common;

namespace JoFit.Contracts.Requests.CourseCategories;

/// <summary>Create and update body of a course category.</summary>
public sealed record CourseCategoryDraftRequest
{
    public string? Slug { get; init; }
    public LocalizedTextRequest Name { get; init; } = new();
    public LocalizedTextRequest? Description { get; init; }
}

public sealed record GetCourseCategoriesPagedRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
    public string? Locale { get; init; }
}
