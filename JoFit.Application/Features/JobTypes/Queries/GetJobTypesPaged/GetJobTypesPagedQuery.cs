using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.JobTypes.Dtos;
using JoFit.Domain.Common.Results;

namespace JoFit.Application.Features.JobTypes.Queries.GetJobTypesPaged;

public sealed record GetJobTypesPagedQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null) : ICachedQuery<Result<PagedResult<JobTypeDto>>>
{
    public string CacheKey =>
        $"{JobTypeCacheTags.JobTypes}:paged:{Page}:{PageSize}:{Search?.Trim().ToLowerInvariant()}:{IsActive}";

    public string[] Tags => [JobTypeCacheTags.JobTypes];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
