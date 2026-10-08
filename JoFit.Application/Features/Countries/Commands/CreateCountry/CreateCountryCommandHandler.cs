using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Countries.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Countries;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.Countries.Commands.CreateCountry;

public class CreateCountryCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<CreateCountryCommand, Result<CountryDto>>
{
    public async Task<Result<CountryDto>> Handle(CreateCountryCommand request, CancellationToken ct)
    {
        var createResult = Country.Create(request.NameAr, request.NameEn, request.IsActive);
        if (createResult.IsError)
        {
            return createResult.Errors;
        }

        var entity = createResult.Value;

        await unitOfWork.GetRepository<Country>().AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(CountryCacheTags.Countries, ct);

        return CountryDto.From(entity);
    }
}
