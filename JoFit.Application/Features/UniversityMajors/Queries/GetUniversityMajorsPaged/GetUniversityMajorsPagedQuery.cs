using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.UniversityMajors.Dtos;
using JoFit.Domain.Common.Results;

namespace JoFit.Application.Features.UniversityMajors.Queries.GetUniversityMajorsPaged;

public sealed record GetUniversityMajorsPagedQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null) : ICachedQuery<Result<PagedResult<UniversityMajorDto>>>
{
    public string CacheKey =>
        $"{UniversityMajorCacheTags.UniversityMajors}:paged:{Page}:{PageSize}:{Search?.Trim().ToLowerInvariant()}:{IsActive}";

    public string[] Tags => [UniversityMajorCacheTags.UniversityMajors];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
