using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Domain.Brands;

namespace JoFit.Application.Features.Brands.Dtos;

/// <summary>Admin view: both languages.</summary>
public sealed record BrandDto(long Id, string Slug, LocalizedText Name, MediaImageDto? Logo, int ProductCount)
{
    public static BrandDto From(Brand entity, int productCount = 0)
        => new(
            entity.Id,
            entity.Slug,
            new LocalizedText(entity.NameEn, entity.NameAr),
            entity.LogoUrl is null ? null : new MediaImageDto(entity.LogoMediaId ?? string.Empty, entity.LogoUrl, entity.LogoAlt ?? string.Empty),
            productCount);
}

/// <summary>Storefront view: text already resolved to the requested locale.</summary>
public sealed record PublicBrandDto(long Id, string Slug, string Name, MediaImageDto? Logo, int ProductCount)
{
    public static PublicBrandDto From(Brand entity, ILocalizationService localization, string? language, int productCount = 0)
        => new(
            entity.Id,
            entity.Slug,
            localization.GetLocalizedName(entity.NameAr, entity.NameEn, language),
            entity.LogoUrl is null ? null : new MediaImageDto(entity.LogoMediaId ?? string.Empty, entity.LogoUrl, entity.LogoAlt ?? string.Empty),
            productCount);
}
