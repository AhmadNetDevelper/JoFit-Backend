using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.AcademicDegrees.Dtos;
using JoFit.Domain.Common.Results;

namespace JoFit.Application.Features.AcademicDegrees.Queries.GetAcademicDegreesPaged;

public sealed record GetAcademicDegreesPagedQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null) : ICachedQuery<Result<PagedResult<AcademicDegreeDto>>>
{
    public string CacheKey =>
        $"{AcademicDegreeCacheTags.AcademicDegrees}:paged:{Page}:{PageSize}:{Search?.Trim().ToLowerInvariant()}:{IsActive}";

    public string[] Tags => [AcademicDegreeCacheTags.AcademicDegrees];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
