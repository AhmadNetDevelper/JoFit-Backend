using JoFit.Contracts.Requests.Common;

namespace JoFit.Contracts.Requests.CoachSpecialisations;

/// <summary>Create and update body of a coach specialisation.</summary>
public sealed record CoachSpecialisationDraftRequest
{
    public LocalizedTextRequest Name { get; init; } = new();
}

public sealed record GetCoachSpecialisationsPagedRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
    public string? Locale { get; init; }
}
