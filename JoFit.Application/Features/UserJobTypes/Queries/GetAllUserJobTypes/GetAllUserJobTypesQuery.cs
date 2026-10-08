using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.UserJobTypes.Dtos;
using JoFit.Domain.Common.Results;

namespace JoFit.Application.Features.UserJobTypes.Queries.GetAllUserJobTypes;

/// <param name="Language">Normalized language code ("ar" or "en"). Part of the cache key so languages never mix.</param>
public sealed record GetAllUserJobTypesQuery(string Language)
    : ICachedQuery<Result<List<UserJobTypeLookupDto>>>
{
    public string CacheKey => $"{UserJobTypeCacheTags.UserJobTypes}:all:{Language}";

    public string[] Tags => [UserJobTypeCacheTags.UserJobTypes];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
