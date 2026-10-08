using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Domain.RentalDepots;

namespace JoFit.Application.Features.RentalDepots.Dtos;

/// <summary>Admin view: both languages.</summary>
public sealed record RentalDepotDto(
    long Id,
    LocalizedText Name,
    LocalizedText City,
    string CountryCode)
{
    public static RentalDepotDto From(RentalDepot e)
        => new(
            e.Id,
            new LocalizedText(e.NameEn, e.NameAr),
            new LocalizedText(e.CityEn, e.CityAr),
            e.CountryCode);
}

/// <summary>Storefront view: text resolved to the requested language.</summary>
public sealed record PublicRentalDepotDto(
    long Id,
    string Name,
    string City,
    string CountryCode)
{
    public static PublicRentalDepotDto From(RentalDepot e, ILocalizationService loc, string? language)
        => new(
            e.Id,
            loc.GetLocalizedName(e.NameAr, e.NameEn, language),
            loc.GetLocalizedName(e.CityAr, e.CityEn, language),
            e.CountryCode);
}
