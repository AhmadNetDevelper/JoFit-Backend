using JoFit.Contracts.Requests.Common;
using JoFit.Contracts.Requests.Courses;

namespace JoFit.Contracts.Requests.Rental;

public sealed record RentalRateRequest
{
    public string Unit { get; init; } = string.Empty;
    public MoneyRequest Price { get; init; } = new();
    public int MinUnits { get; init; } = 1;
    public int? MaxUnits { get; init; }
}

/// <summary>Create and update body of a rental item. Its status is set here; there is no separate status endpoint.</summary>
public sealed record RentalItemDraftRequest
{
    public string Slug { get; init; } = string.Empty;
    public string Status { get; init; } = "draft";
    public LocalizedTextRequest Title { get; init; } = new();
    public LocalizedTextRequest Summary { get; init; } = new();
    public LocalizedTextRequest? Description { get; init; }
    public string Condition { get; init; } = string.Empty;
    public long DepotId { get; init; }
    public List<long> CategoryIds { get; init; } = [];
    public int TotalUnits { get; init; }
    public int UnitsOutOfService { get; init; }

    /// <summary>Per unit hired. Left out means no deposit; on update, leaving it out removes it.</summary>
    public MoneyRequest? Deposit { get; init; }
    public List<RentalRateRequest> Rates { get; init; } = [];

    /// <summary>Absent keeps the gallery, a list replaces it, an empty list clears it.</summary>
    public List<MediaImageRequest>? Media { get; init; }
}

public sealed record OutOfServiceRequest
{
    public int UnitsOutOfService { get; init; }
}

public sealed record RentalPeriodRequest
{
    public string? Unit { get; init; }

    /// <summary>yyyy-MM-dd.</summary>
    public string? From { get; init; }
    public string? To { get; init; }
    public int Quantity { get; init; } = 1;
}

public sealed record GetAdminRentalItemsRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string[]? Statuses { get; init; }
    public long[]? CategoryIds { get; init; }
    public long[]? DepotIds { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
}

public sealed record GetAdminRentalBookingsRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string[]? Statuses { get; init; }
    public long[]? ItemIds { get; init; }
    public long? CustomerId { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
}

public sealed record GetPublicRentalItemsRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string[]? CategorySlugs { get; init; }
    public string? Sort { get; init; }
}
