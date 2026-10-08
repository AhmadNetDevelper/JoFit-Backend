using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Domain.RentalCategories;

namespace JoFit.Application.Features.RentalCategories.Dtos;

/// <summary>Admin view: both languages.</summary>
public sealed record RentalCategoryDto(
    long Id,
    string? Slug,
    LocalizedText Name,
    LocalizedText? Description,
    MediaImageDto? Image,
    long? ParentId)
{
    public static RentalCategoryDto From(RentalCategory e)
        => new(
            e.Id,
            e.Slug,
            new LocalizedText(e.NameEn, e.NameAr),
            e.DescriptionEn is null || e.DescriptionAr is null ? null : new LocalizedText(e.DescriptionEn, e.DescriptionAr),
            e.ImageUrl is null ? null : new MediaImageDto(e.ImageMediaId ?? string.Empty, e.ImageUrl, e.ImageAlt ?? string.Empty),
            e.ParentId);
}

/// <summary>Storefront view: text resolved to the requested language.</summary>
public sealed record PublicRentalCategoryDto(
    long Id,
    string? Slug,
    string Name,
    string? Description,
    MediaImageDto? Image,
    long? ParentId)
{
    public static PublicRentalCategoryDto From(RentalCategory e, ILocalizationService loc, string? language)
        => new(
            e.Id,
            e.Slug,
            loc.GetLocalizedName(e.NameAr, e.NameEn, language),
            e.DescriptionEn is null || e.DescriptionAr is null ? null : loc.GetLocalizedName(e.DescriptionAr, e.DescriptionEn, language),
            e.ImageUrl is null ? null : new MediaImageDto(e.ImageMediaId ?? string.Empty, e.ImageUrl, e.ImageAlt ?? string.Empty),
            e.ParentId);
}
