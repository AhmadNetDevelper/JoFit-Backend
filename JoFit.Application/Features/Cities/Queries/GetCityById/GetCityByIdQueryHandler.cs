using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Cities.Dtos;
using JoFit.Domain.Cities;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Cities.Queries.GetCityById;

public class GetCityByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCityByIdQuery, Result<CityDto>>
{
    public async Task<Result<CityDto>> Handle(GetCityByIdQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<City>()
            .GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return CityErrors.CityNotFound;
        }

        return CityDto.From(entity);
    }
}
