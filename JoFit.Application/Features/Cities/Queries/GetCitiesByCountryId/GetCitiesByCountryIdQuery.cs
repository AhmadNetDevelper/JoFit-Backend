using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Cities.Dtos;
using JoFit.Domain.Common.Results;

namespace JoFit.Application.Features.Cities.Queries.GetCitiesByCountryId;

/// <param name="CountryId">The country whose active cities are returned.</param>
/// <param name="Language">Normalized language code ("ar" or "en"). Part of the cache key so languages never mix.</param>
public sealed record GetCitiesByCountryIdQuery(long CountryId, string Language)
    : ICachedQuery<Result<List<CityLookupDto>>>
{
    public string CacheKey => $"{CityCacheTags.Cities}:country:{CountryId}:{Language}";

    public string[] Tags => [CityCacheTags.Cities];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
