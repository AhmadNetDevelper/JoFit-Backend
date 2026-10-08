using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Cities.Dtos;
using JoFit.Domain.Cities;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Countries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.Cities.Commands.CreateCity;

public class CreateCityCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<CreateCityCommand, Result<CityDto>>
{
    public async Task<Result<CityDto>> Handle(CreateCityCommand request, CancellationToken ct)
    {
        var countryExists = await unitOfWork.GetRepository<Country>()
            .GetTableNoTracking()
            .AnyAsync(x => x.Id == request.CountryId && !x.IsDeleted, ct);

        if (!countryExists)
        {
            return CountryErrors.CountryNotFound;
        }

        var createResult = City.Create(request.NameAr, request.NameEn, request.CountryId, request.IsActive);
        if (createResult.IsError)
        {
            return createResult.Errors;
        }

        var entity = createResult.Value;

        await unitOfWork.GetRepository<City>().AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(CityCacheTags.Cities, ct);

        return CityDto.From(entity);
    }
}
