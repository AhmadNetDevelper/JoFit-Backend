using JoFit.Application.Common.Interfaces;
using JoFit.Domain.Cities;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.Cities.Commands.DeleteCity;

public class DeleteCityCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<DeleteCityCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteCityCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<City>();

        var entity = await repository.GetTableAsTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return CityErrors.CityNotFound;
        }

        // Soft delete: flags IsDeleted and keeps the row
        repository.DeleteAsync(entity);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(CityCacheTags.Cities, ct);

        return Result.Deleted;
    }
}
