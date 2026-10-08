using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Courses.Catalogue;
using JoFit.Domain.Products;

namespace JoFit.Application.Features.Products;

public sealed record AdminVariantDto(
    long Id,
    string Sku,
    LocalizedText Title,
    MoneyDto Price,
    int StockOnHand,
    int LowStockThreshold,
    bool IsActive,
    bool IsLowStock)
{
    public static AdminVariantDto From(ProductVariant v)
        => new(v.Id, v.Sku, new LocalizedText(v.TitleEn, v.TitleAr), new MoneyDto(v.PriceAmount), v.StockOnHand, v.LowStockThreshold, v.IsActive, v.IsLowStock);
}

public sealed record AdminProductDto(
    long Id,
    string Slug,
    LocalizedText Title,
    LocalizedText Summary,
    LocalizedText? Description,
    string Status,
    long BrandId,
    IReadOnlyList<long> CategoryIds,
    IReadOnlyList<long> SportIds,
    IReadOnlyList<AdminVariantDto> Variants,
    IReadOnlyList<MediaImageDto> Media,
    int TotalStock,
    DateTimeOffset? PublishedAt,
    AuditDto Audit)
{
    public static AdminProductDto From(
        Product p, IReadOnlyList<long> categoryIds, IReadOnlyList<long> sportIds, IReadOnlyList<MediaImageDto> media)
        => new(
            p.Id,
            p.Slug,
            new LocalizedText(p.TitleEn, p.TitleAr),
            new LocalizedText(p.SummaryEn, p.SummaryAr),
            p.DescriptionEn is null || p.DescriptionAr is null ? null : new LocalizedText(p.DescriptionEn, p.DescriptionAr),
            p.Status,
            p.BrandId,
            categoryIds,
            sportIds,
            p.Variants.OrderBy(v => v.Position).Select(AdminVariantDto.From).ToList(),
            media,
            p.Variants.Sum(v => v.StockOnHand),
            p.PublishedAtUtc,
            AuditDto.From(p));
}

public sealed record PublicVariantDto(long Id, string Sku, string Title, MoneyDto Price, string StockStatus)
{
    public static string StatusOf(ProductVariant v) => v.StockOnHand <= 0 ? "out-of-stock" : v.IsLowStock ? "low-stock" : "in-stock";

    public static PublicVariantDto From(ProductVariant v, ILocalizationService loc, string? language)
        => new(v.Id, v.Sku, loc.GetLocalizedName(v.TitleAr, v.TitleEn, language), new MoneyDto(v.PriceAmount), StatusOf(v));
}

public sealed record RefDto(long Id, string Slug, string Name);

public sealed record PublicProductDto(
    long Id,
    string Slug,
    string Title,
    string Summary,
    string? Description,
    RefDto? Brand,
    IReadOnlyList<RefDto> Categories,
    IReadOnlyList<RefDto> Sports,
    IReadOnlyList<PublicVariantDto> Variants,
    MoneyDto Price,
    string StockStatus,
    IReadOnlyList<MediaImageDto> Media,
    DateTimeOffset? PublishedAt)
{
    public static PublicProductDto From(
        Product p,
        ILocalizationService loc,
        string? language,
        RefDto? brand,
        IReadOnlyList<RefDto> categories,
        IReadOnlyList<RefDto> sports,
        IReadOnlyList<MediaImageDto> media)
    {
        var variants = p.Variants.Where(v => v.IsActive).OrderBy(v => v.Position).ToList();
        var stock = variants.Count == 0 || variants.All(v => v.StockOnHand <= 0)
            ? "out-of-stock"
            : variants.Any(v => v.IsLowStock) ? "low-stock" : "in-stock";
        return new PublicProductDto(
            p.Id,
            p.Slug,
            loc.GetLocalizedName(p.TitleAr, p.TitleEn, language),
            loc.GetLocalizedName(p.SummaryAr, p.SummaryEn, language),
            p.DescriptionEn is null || p.DescriptionAr is null ? null : loc.GetLocalizedName(p.DescriptionAr, p.DescriptionEn, language),
            brand,
            categories,
            sports,
            variants.Select(v => PublicVariantDto.From(v, loc, language)).ToList(),
            new MoneyDto(variants.Count == 0 ? 0 : variants.Min(v => v.PriceAmount)),
            stock,
            media,
            p.PublishedAtUtc);
    }
}
