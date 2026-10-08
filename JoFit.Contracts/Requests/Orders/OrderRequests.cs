namespace JoFit.Contracts.Requests.Orders;

/// <summary>A basket line. shop: productId, variantId, quantity. rental: rentalItemId, unit, from, to, quantity.</summary>
public sealed record OrderLineRequest
{
    public string? Kind { get; init; }
    public long? ProductId { get; init; }
    public long? VariantId { get; init; }
    public long? RentalItemId { get; init; }
    public string? Unit { get; init; }

    /// <summary>yyyy-MM-dd.</summary>
    public string? From { get; init; }
    public string? To { get; init; }
    public int Quantity { get; init; } = 1;
}

public sealed record PlaceOrderRequest
{
    public List<OrderLineRequest> Lines { get; init; } = [];

    /// <summary>delivery or pickup.</summary>
    public string? Fulfilment { get; init; }
    public long? AddressId { get; init; }
    public string? Notes { get; init; }
}

public sealed record AddressRequest
{
    public string? Recipient { get; init; }
    public string? Phone { get; init; }
    public string? Line1 { get; init; }
    public string? Line2 { get; init; }
    public string? City { get; init; }
    public string? Notes { get; init; }
}

public sealed record GetAdminOrdersRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string[]? Statuses { get; init; }
    public long? CustomerId { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
}

public sealed record CancelSessionRequest
{
    public string? Reason { get; init; }
}

public sealed record CustomerDraftRequest
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string Locale { get; init; } = "en";
    public bool MarketingOptIn { get; init; }
}

public sealed record GetAdminCustomersRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public bool MarketingOptInOnly { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
}
