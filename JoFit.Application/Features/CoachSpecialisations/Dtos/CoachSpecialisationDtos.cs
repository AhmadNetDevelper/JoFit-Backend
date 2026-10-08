using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Domain.CoachSpecialisations;

namespace JoFit.Application.Features.CoachSpecialisations.Dtos;

/// <summary>Admin view: both languages.</summary>
public sealed record CoachSpecialisationDto(
    long Id,
    LocalizedText Name)
{
    public static CoachSpecialisationDto From(CoachSpecialisation e)
        => new(
            e.Id,
            new LocalizedText(e.NameEn, e.NameAr));
}

/// <summary>Storefront view: text resolved to the requested language.</summary>
public sealed record PublicCoachSpecialisationDto(
    long Id,
    string Name)
{
    public static PublicCoachSpecialisationDto From(CoachSpecialisation e, ILocalizationService loc, string? language)
        => new(
            e.Id,
            loc.GetLocalizedName(e.NameAr, e.NameEn, language));
}
