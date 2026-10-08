using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Domain.ArticleCategories;

namespace JoFit.Application.Features.ArticleCategories.Dtos;

/// <summary>Admin view: both languages.</summary>
public sealed record ArticleCategoryDto(
    long Id,
    string? Slug,
    LocalizedText Name,
    LocalizedText? Description)
{
    public static ArticleCategoryDto From(ArticleCategory e)
        => new(
            e.Id,
            e.Slug,
            new LocalizedText(e.NameEn, e.NameAr),
            e.DescriptionEn is null || e.DescriptionAr is null ? null : new LocalizedText(e.DescriptionEn, e.DescriptionAr));
}

/// <summary>Storefront view: text resolved to the requested language.</summary>
public sealed record PublicArticleCategoryDto(
    long Id,
    string? Slug,
    string Name,
    string? Description)
{
    public static PublicArticleCategoryDto From(ArticleCategory e, ILocalizationService loc, string? language)
        => new(
            e.Id,
            e.Slug,
            loc.GetLocalizedName(e.NameAr, e.NameEn, language),
            e.DescriptionEn is null || e.DescriptionAr is null ? null : loc.GetLocalizedName(e.DescriptionAr, e.DescriptionEn, language));
}
