using JoFit.Contracts.Requests.Common;

namespace JoFit.Contracts.Requests.RentalDepots;

/// <summary>Create and update body of a rental depot.</summary>
public sealed record RentalDepotDraftRequest
{
    public LocalizedTextRequest Name { get; init; } = new();
    public LocalizedTextRequest City { get; init; } = new();
    public string CountryCode { get; init; } = string.Empty;
}

public sealed record GetRentalDepotsPagedRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
    public string? Locale { get; init; }
}
