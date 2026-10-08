using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.JobStatuses.Dtos;
using JoFit.Domain.Common.Results;

namespace JoFit.Application.Features.JobStatuses.Queries.GetJobStatusesPaged;

public sealed record GetJobStatusesPagedQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null) : ICachedQuery<Result<PagedResult<JobStatusDto>>>
{
    public string CacheKey =>
        $"{JobStatusCacheTags.JobStatuses}:paged:{Page}:{PageSize}:{Search?.Trim().ToLowerInvariant()}:{IsActive}";

    public string[] Tags => [JobStatusCacheTags.JobStatuses];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
