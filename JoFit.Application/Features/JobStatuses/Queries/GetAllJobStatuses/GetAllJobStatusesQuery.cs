using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.JobStatuses.Dtos;
using JoFit.Domain.Common.Results;

namespace JoFit.Application.Features.JobStatuses.Queries.GetAllJobStatuses;

/// <param name="Language">Normalized language code ("ar" or "en"). Part of the cache key so languages never mix.</param>
public sealed record GetAllJobStatusesQuery(string Language)
    : ICachedQuery<Result<List<JobStatusLookupDto>>>
{
    public string CacheKey => $"{JobStatusCacheTags.JobStatuses}:all:{Language}";

    public string[] Tags => [JobStatusCacheTags.JobStatuses];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
