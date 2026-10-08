using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Countries.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Countries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Countries.Queries.GetAllCountries;

public class GetAllCountriesQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localizationService)
    : IRequestHandler<GetAllCountriesQuery, Result<List<CountryLookupDto>>>
{
    public async Task<Result<List<CountryLookupDto>>> Handle(GetAllCountriesQuery request, CancellationToken ct)
    {
        var rows = await unitOfWork.GetRepository<Country>()
            .GetTableNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.NameAr, x.NameEn })
            .ToListAsync(ct);

        return rows
            .ConvertAll(x => new CountryLookupDto(
                x.Id,
                localizationService.GetLocalizedName(x.NameAr, x.NameEn, request.Language)));
    }
}
