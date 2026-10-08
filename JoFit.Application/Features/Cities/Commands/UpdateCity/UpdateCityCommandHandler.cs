using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Cities.Dtos;
using JoFit.Domain.Cities;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Countries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.Cities.Commands.UpdateCity;

public class UpdateCityCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<UpdateCityCommand, Result<CityDto>>
{
    public async Task<Result<CityDto>> Handle(UpdateCityCommand request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<City>()
            .GetTableAsTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return CityErrors.CityNotFound;
        }

        var countryExists = await unitOfWork.GetRepository<Country>()
            .GetTableNoTracking()
            .AnyAsync(x => x.Id == request.CountryId && !x.IsDeleted, ct);

        if (!countryExists)
        {
            return CountryErrors.CountryNotFound;
        }

        var updateResult = entity.Update(request.NameAr, request.NameEn, request.CountryId, request.IsActive);
        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(CityCacheTags.Cities, ct);

        return CityDto.From(entity);
    }
}
