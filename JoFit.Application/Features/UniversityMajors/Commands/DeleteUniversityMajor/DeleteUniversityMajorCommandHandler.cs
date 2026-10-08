using JoFit.Application.Common.Interfaces;
using JoFit.Domain.Common.Results;
using JoFit.Domain.UniversityMajors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.UniversityMajors.Commands.DeleteUniversityMajor;

public class DeleteUniversityMajorCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<DeleteUniversityMajorCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteUniversityMajorCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<UniversityMajor>();

        var entity = await repository.GetTableAsTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return UniversityMajorErrors.UniversityMajorNotFound;
        }

        // Soft delete: flags IsDeleted and keeps the row
        repository.DeleteAsync(entity);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(UniversityMajorCacheTags.UniversityMajors, ct);

        return Result.Deleted;
    }
}
