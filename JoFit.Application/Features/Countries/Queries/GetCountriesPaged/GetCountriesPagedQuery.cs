using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Countries.Dtos;
using JoFit.Domain.Common.Results;

namespace JoFit.Application.Features.Countries.Queries.GetCountriesPaged;

public sealed record GetCountriesPagedQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null) : ICachedQuery<Result<PagedResult<CountryDto>>>
{
    public string CacheKey =>
        $"{CountryCacheTags.Countries}:paged:{Page}:{PageSize}:{Search?.Trim().ToLowerInvariant()}:{IsActive}";

    public string[] Tags => [CountryCacheTags.Countries];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
