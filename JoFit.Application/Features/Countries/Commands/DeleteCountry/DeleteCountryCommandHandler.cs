using JoFit.Application.Common.Interfaces;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Countries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.Countries.Commands.DeleteCountry;

public class DeleteCountryCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<DeleteCountryCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteCountryCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<Country>();

        var entity = await repository.GetTableAsTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return CountryErrors.CountryNotFound;
        }

        // Soft delete: flags IsDeleted and keeps the row
        repository.DeleteAsync(entity);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(CountryCacheTags.Countries, ct);

        return Result.Deleted;
    }
}
