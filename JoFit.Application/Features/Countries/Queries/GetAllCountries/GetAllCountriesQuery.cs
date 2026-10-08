using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Countries.Dtos;
using JoFit.Domain.Common.Results;

namespace JoFit.Application.Features.Countries.Queries.GetAllCountries;

/// <param name="Language">Normalized language code ("ar" or "en"). Part of the cache key so languages never mix.</param>
public sealed record GetAllCountriesQuery(string Language)
    : ICachedQuery<Result<List<CountryLookupDto>>>
{
    public string CacheKey => $"{CountryCacheTags.Countries}:all:{Language}";

    public string[] Tags => [CountryCacheTags.Countries];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
