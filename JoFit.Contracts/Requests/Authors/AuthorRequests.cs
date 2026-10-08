using JoFit.Contracts.Requests.Common;

namespace JoFit.Contracts.Requests.Authors;

/// <summary>Create and update body of a author.</summary>
public sealed record AuthorDraftRequest
{
    public string? Slug { get; init; }
    public string Name { get; init; } = string.Empty;
    public LocalizedTextRequest? Role { get; init; }
    public LocalizedTextRequest? Bio { get; init; }
    public MediaImageRequest? Avatar { get; init; }
}

public sealed record GetAuthorsPagedRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
    public string? Locale { get; init; }
}
