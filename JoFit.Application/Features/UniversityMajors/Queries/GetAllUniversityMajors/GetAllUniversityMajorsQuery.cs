using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.UniversityMajors.Dtos;
using JoFit.Domain.Common.Results;

namespace JoFit.Application.Features.UniversityMajors.Queries.GetAllUniversityMajors;

/// <param name="Language">Normalized language code ("ar" or "en"). Part of the cache key so languages never mix.</param>
public sealed record GetAllUniversityMajorsQuery(string Language)
    : ICachedQuery<Result<List<UniversityMajorLookupDto>>>
{
    public string CacheKey => $"{UniversityMajorCacheTags.UniversityMajors}:all:{Language}";

    public string[] Tags => [UniversityMajorCacheTags.UniversityMajors];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
