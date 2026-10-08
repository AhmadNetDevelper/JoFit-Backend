using JoFit.Contracts.Requests.Common;
using JoFit.Contracts.Requests.Courses;

namespace JoFit.Contracts.Requests.Products;

/// <summary>Create and update body of a product. There are no variants here: they are created with the product.</summary>
public sealed record ProductDraftRequest
{
    public LocalizedTextRequest Title { get; init; } = new();
    public LocalizedTextRequest Summary { get; init; } = new();
    public LocalizedTextRequest? Description { get; init; }
    public long BrandId { get; init; }
    public List<long> CategoryIds { get; init; } = [];
    public List<long> SportIds { get; init; } = [];
    public string Status { get; init; } = "draft";

    /// <summary>Absent keeps the gallery, a list replaces it, an empty list clears it.</summary>
    public List<MediaImageRequest>? Media { get; init; }
}

public sealed record NewVariantRequest
{
    public string? Sku { get; init; }
    public MoneyRequest Price { get; init; } = new();
    public int StockOnHand { get; init; }
}

/// <summary>A new product: the draft plus its first variant.</summary>
public sealed record NewProductDraftRequest
{
    public LocalizedTextRequest Title { get; init; } = new();
    public LocalizedTextRequest Summary { get; init; } = new();
    public LocalizedTextRequest? Description { get; init; }
    public long BrandId { get; init; }
    public List<long> CategoryIds { get; init; } = [];
    public List<long> SportIds { get; init; } = [];
    public string Status { get; init; } = "draft";
    public List<MediaImageRequest>? Media { get; init; }
    public NewVariantRequest Variant { get; init; } = new();
}

public sealed record StockAdjustmentRequest
{
    public long VariantId { get; init; }

    /// <summary>A signed change, never an absolute value.</summary>
    public int Delta { get; init; }
    public string? Reason { get; init; }
}

public sealed record GetAdminProductsRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string[]? Statuses { get; init; }
    public long[]? CategoryIds { get; init; }
    public long[]? BrandIds { get; init; }
    public long[]? SportIds { get; init; }
    public string[]? StockStatuses { get; init; }
    public bool LowStockOnly { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
}

public sealed record GetPublicProductsRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string[]? CategorySlugs { get; init; }
    public string[]? BrandSlugs { get; init; }
    public string[]? SportSlugs { get; init; }
    public string[]? StockStatuses { get; init; }
    public string? Sort { get; init; }
}
