using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Countries.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Countries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.Countries.Commands.UpdateCountry;

public class UpdateCountryCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<UpdateCountryCommand, Result<CountryDto>>
{
    public async Task<Result<CountryDto>> Handle(UpdateCountryCommand request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<Country>()
            .GetTableAsTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return CountryErrors.CountryNotFound;
        }

        var updateResult = entity.Update(request.NameAr, request.NameEn, request.IsActive);
        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(CountryCacheTags.Countries, ct);

        return CountryDto.From(entity);
    }
}
