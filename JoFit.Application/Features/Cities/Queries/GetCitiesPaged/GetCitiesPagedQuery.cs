using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Cities.Dtos;
using JoFit.Domain.Common.Results;

namespace JoFit.Application.Features.Cities.Queries.GetCitiesPaged;

public sealed record GetCitiesPagedQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null,
    long? CountryId = null) : ICachedQuery<Result<PagedResult<CityDto>>>
{
    public string CacheKey =>
        $"{CityCacheTags.Cities}:paged:{Page}:{PageSize}:{Search?.Trim().ToLowerInvariant()}:{IsActive}:{CountryId}";

    public string[] Tags => [CityCacheTags.Cities];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
