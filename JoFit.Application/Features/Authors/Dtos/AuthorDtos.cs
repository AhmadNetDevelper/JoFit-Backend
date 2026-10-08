using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Domain.Authors;

namespace JoFit.Application.Features.Authors.Dtos;

/// <summary>Admin view: both languages.</summary>
public sealed record AuthorDto(
    long Id,
    string? Slug,
    string Name,
    LocalizedText? Role,
    LocalizedText? Bio,
    MediaImageDto? Avatar)
{
    public static AuthorDto From(Author e)
        => new(
            e.Id,
            e.Slug,
            e.Name,
            e.RoleEn is null || e.RoleAr is null ? null : new LocalizedText(e.RoleEn, e.RoleAr),
            e.BioEn is null || e.BioAr is null ? null : new LocalizedText(e.BioEn, e.BioAr),
            e.AvatarUrl is null ? null : new MediaImageDto(e.AvatarMediaId ?? string.Empty, e.AvatarUrl, e.AvatarAlt ?? string.Empty));
}

/// <summary>Storefront view: text resolved to the requested language.</summary>
public sealed record PublicAuthorDto(
    long Id,
    string? Slug,
    string Name,
    string? Role,
    string? Bio,
    MediaImageDto? Avatar)
{
    public static PublicAuthorDto From(Author e, ILocalizationService loc, string? language)
        => new(
            e.Id,
            e.Slug,
            e.Name,
            e.RoleEn is null || e.RoleAr is null ? null : loc.GetLocalizedName(e.RoleAr, e.RoleEn, language),
            e.BioEn is null || e.BioAr is null ? null : loc.GetLocalizedName(e.BioAr, e.BioEn, language),
            e.AvatarUrl is null ? null : new MediaImageDto(e.AvatarMediaId ?? string.Empty, e.AvatarUrl, e.AvatarAlt ?? string.Empty));
}
