using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Products;

public static class ProductStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";
    public const string Archived = "archived";

    public static readonly string[] All = [Draft, Published, Archived];
}

/// <summary>The editable fields of a product, as the back-office form saves them.</summary>
public sealed record ProductDetails(
    string TitleEn,
    string TitleAr,
    string SummaryEn,
    string SummaryAr,
    string? DescriptionEn,
    string? DescriptionAr,
    long BrandId,
    string Status);

public class Product : AuditableEntity
{
    public string Slug { get; private set; } = string.Empty;
    public string TitleEn { get; private set; } = string.Empty;
    public string TitleAr { get; private set; } = string.Empty;
    public string SummaryEn { get; private set; } = string.Empty;
    public string SummaryAr { get; private set; } = string.Empty;
    public string? DescriptionEn { get; private set; }
    public string? DescriptionAr { get; private set; }
    public string Status { get; private set; } = ProductStatuses.Draft;
    public long BrandId { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public List<ProductVariant> Variants { get; private set; } = [];

    private Product()
    {
    }

    public bool IsPublished => Status == ProductStatuses.Published;

    public static Result<Product> Create(ProductDetails details, string slug)
    {
        var product = new Product { Slug = slug };
        var applied = product.Apply(details);
        return applied.IsError ? applied.Errors : product;
    }

    /// <summary>
    /// A draft needs the English title and summary; a published product needs title, summary and description
    /// in every language. A published product keeps being checked, so a language cannot be blanked later.
    /// </summary>
    public Result<Updated> Apply(ProductDetails d)
    {
        var errors = new List<Error>();
        var titleEn = (d.TitleEn ?? string.Empty).Trim();
        var titleAr = (d.TitleAr ?? string.Empty).Trim();
        var summaryEn = (d.SummaryEn ?? string.Empty).Trim();
        var summaryAr = (d.SummaryAr ?? string.Empty).Trim();
        var descriptionEn = string.IsNullOrWhiteSpace(d.DescriptionEn) ? null : d.DescriptionEn.Trim();
        var descriptionAr = string.IsNullOrWhiteSpace(d.DescriptionAr) ? null : d.DescriptionAr.Trim();

        if (!ProductStatuses.All.Contains(d.Status)) errors.Add(ProductErrors.Field("status", "Choose draft, published or archived."));
        if (titleEn.Length < 3) errors.Add(ProductErrors.Field("title.en", "The English title needs at least 3 characters."));
        if (titleAr.Length < 3) errors.Add(ProductErrors.Field("title.ar", "The Arabic title needs at least 3 characters."));
        if (titleEn.Length > 150) errors.Add(ProductErrors.Field("title.en", "A title cannot exceed 150 characters."));
        if (titleAr.Length > 150) errors.Add(ProductErrors.Field("title.ar", "A title cannot exceed 150 characters."));
        if (summaryEn.Length == 0) errors.Add(ProductErrors.Field("summary.en", "The English summary is required."));
        if (d.BrandId <= 0) errors.Add(ProductErrors.Field("brandId", "Choose a brand."));

        if (d.Status == ProductStatuses.Published)
        {
            if (summaryAr.Length == 0) errors.Add(ProductErrors.Field("summary.ar", "The Arabic summary is required to publish."));
            if (descriptionEn is null) errors.Add(ProductErrors.Field("description.en", "The English description is required to publish."));
            if (descriptionAr is null) errors.Add(ProductErrors.Field("description.ar", "The Arabic description is required to publish."));
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        TitleEn = titleEn;
        TitleAr = titleAr;
        SummaryEn = summaryEn;
        SummaryAr = summaryAr;
        DescriptionEn = descriptionEn;
        DescriptionAr = descriptionAr;
        BrandId = d.BrandId;
        return SetStatus(d.Status);
    }

    /// <summary>Publishing re-checks only the description in every language.</summary>
    public Result<Updated> SetStatus(string status)
    {
        if (!ProductStatuses.All.Contains(status))
        {
            return ProductErrors.Field("status", "Choose draft, published or archived.");
        }

        if (status == ProductStatuses.Published && (string.IsNullOrWhiteSpace(DescriptionEn) || string.IsNullOrWhiteSpace(DescriptionAr)))
        {
            return ProductErrors.Field("description", "Add the description in every language before publishing.");
        }

        if (status == ProductStatuses.Published)
        {
            PublishedAtUtc ??= DateTimeOffset.UtcNow;
        }

        Status = status;
        return Result.Updated;
    }

    public ProductVariant AddVariant(string sku, long priceAmount, int stockOnHand)
    {
        var variant = ProductVariant.Create(sku, priceAmount, stockOnHand, Variants.Count);
        Variants.Add(variant);
        return variant;
    }
}

public class ProductVariant : Entity
{
    public long ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;

    /// <summary>Upper-cased SKU, so uniqueness ignores case.</summary>
    public string SkuKey { get; private set; } = string.Empty;
    public string TitleEn { get; private set; } = "Standard";
    public string TitleAr { get; private set; } = "قياسي";

    /// <summary>Minor units of JOD (3 decimals): 28000 is 28.000 JOD.</summary>
    public long PriceAmount { get; private set; }
    public int StockOnHand { get; private set; }
    public int LowStockThreshold { get; private set; } = 4;
    public bool IsActive { get; private set; } = true;
    public int Position { get; private set; }

    private ProductVariant()
    {
    }

    public static ProductVariant Create(string sku, long priceAmount, int stockOnHand, int position)
        => new() { Sku = sku.Trim(), SkuKey = sku.Trim().ToUpperInvariant(), PriceAmount = priceAmount, StockOnHand = stockOnHand, Position = position };

    public bool IsLowStock => StockOnHand <= LowStockThreshold;

    /// <summary>A signed change; stock may never go below zero.</summary>
    public Result<Updated> Adjust(int delta)
    {
        if (StockOnHand + delta < 0)
        {
            return ProductErrors.Field("delta", $"Only {StockOnHand} in stock; that would go below 0.");
        }

        StockOnHand += delta;
        return Result.Updated;
    }
}

/// <summary>An audit row for every stock change made by staff.</summary>
public class StockAdjustment : Entity
{
    public long ProductVariantId { get; private set; }
    public int Delta { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public long? AdjustedBy { get; private set; }
    public DateTimeOffset AdjustedAtUtc { get; private set; }
    public int StockAfter { get; private set; }

    private StockAdjustment()
    {
    }

    public static StockAdjustment Create(long variantId, int delta, string reason, long? adjustedBy, int stockAfter)
        => new()
        {
            ProductVariantId = variantId,
            Delta = delta,
            Reason = reason,
            AdjustedBy = adjustedBy,
            AdjustedAtUtc = DateTimeOffset.UtcNow,
            StockAfter = stockAfter
        };
}

public class ProductCategoryLink
{
    public long ProductId { get; set; }
    public long ProductCategoryId { get; set; }
}

public class ProductSportLink
{
    public long ProductId { get; set; }
    public long SportId { get; set; }
}
