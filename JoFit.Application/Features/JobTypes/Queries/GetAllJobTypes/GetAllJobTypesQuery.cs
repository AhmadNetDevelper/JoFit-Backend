using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.JobTypes.Dtos;
using JoFit.Domain.Common.Results;

namespace JoFit.Application.Features.JobTypes.Queries.GetAllJobTypes;

/// <param name="Language">Normalized language code ("ar" or "en"). Part of the cache key so languages never mix.</param>
public sealed record GetAllJobTypesQuery(string Language)
    : ICachedQuery<Result<List<JobTypeLookupDto>>>
{
    public string CacheKey => $"{JobTypeCacheTags.JobTypes}:all:{Language}";

    public string[] Tags => [JobTypeCacheTags.JobTypes];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
