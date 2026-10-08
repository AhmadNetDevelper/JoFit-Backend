using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Countries.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Countries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Countries.Queries.GetCountryById;

public class GetCountryByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCountryByIdQuery, Result<CountryDto>>
{
    public async Task<Result<CountryDto>> Handle(GetCountryByIdQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<Country>()
            .GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return CountryErrors.CountryNotFound;
        }

        return CountryDto.From(entity);
    }
}
