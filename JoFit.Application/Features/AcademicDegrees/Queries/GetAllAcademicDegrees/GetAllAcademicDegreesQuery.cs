using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.AcademicDegrees.Dtos;
using JoFit.Domain.Common.Results;

namespace JoFit.Application.Features.AcademicDegrees.Queries.GetAllAcademicDegrees;

/// <param name="Language">Normalized language code ("ar" or "en"). Part of the cache key so languages never mix.</param>
public sealed record GetAllAcademicDegreesQuery(string Language)
    : ICachedQuery<Result<List<AcademicDegreeLookupDto>>>
{
    public string CacheKey => $"{AcademicDegreeCacheTags.AcademicDegrees}:all:{Language}";

    public string[] Tags => [AcademicDegreeCacheTags.AcademicDegrees];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
