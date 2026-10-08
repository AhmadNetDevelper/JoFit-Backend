using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Cities.Dtos;
using JoFit.Domain.Cities;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Cities.Queries.GetCitiesPaged;

public class GetCitiesPagedQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCitiesPagedQuery, Result<PagedResult<CityDto>>>
{
    public async Task<Result<PagedResult<CityDto>>> Handle(GetCitiesPagedQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<City>()
            .GetTableNoTracking()
            .Where(x => !x.IsDeleted);

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        if (request.CountryId.HasValue)
        {
            query = query.Where(x => x.CountryId == request.CountryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.NameAr.Contains(search) || x.NameEn.Contains(search));
        }

        var totalCount = await query.CountAsync(ct);

        var entities = await query
            .OrderBy(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var items = entities.ConvertAll(CityDto.From);

        return new PagedResult<CityDto>(items, request.Page, request.PageSize, totalCount);
    }
}
