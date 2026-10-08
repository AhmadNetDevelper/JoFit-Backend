using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Cities.Dtos;
using JoFit.Domain.Cities;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Cities.Queries.GetCitiesByCountryId;

public class GetCitiesByCountryIdQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localizationService)
    : IRequestHandler<GetCitiesByCountryIdQuery, Result<List<CityLookupDto>>>
{
    public async Task<Result<List<CityLookupDto>>> Handle(GetCitiesByCountryIdQuery request, CancellationToken ct)
    {
        var rows = await unitOfWork.GetRepository<City>()
            .GetTableNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive && x.CountryId == request.CountryId)
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.NameAr, x.NameEn })
            .ToListAsync(ct);

        return rows
            .ConvertAll(x => new CityLookupDto(
                x.Id,
                localizationService.GetLocalizedName(x.NameAr, x.NameEn, request.Language)));
    }
}
