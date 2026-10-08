using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.UserJobTypes.Dtos;
using JoFit.Domain.Common.Results;

namespace JoFit.Application.Features.UserJobTypes.Queries.GetUserJobTypesPaged;

public sealed record GetUserJobTypesPagedQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null) : ICachedQuery<Result<PagedResult<UserJobTypeDto>>>
{
    public string CacheKey =>
        $"{UserJobTypeCacheTags.UserJobTypes}:paged:{Page}:{PageSize}:{Search?.Trim().ToLowerInvariant()}:{IsActive}";

    public string[] Tags => [UserJobTypeCacheTags.UserJobTypes];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
