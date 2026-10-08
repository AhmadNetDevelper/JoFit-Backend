using JoFit.Application.Common.Interfaces;
using JoFit.Domain.Common.Results;
using JoFit.Domain.AcademicDegrees;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.AcademicDegrees.Commands.DeleteAcademicDegree;

public class DeleteAcademicDegreeCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<DeleteAcademicDegreeCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteAcademicDegreeCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<AcademicDegree>();

        var entity = await repository.GetTableAsTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return AcademicDegreeErrors.AcademicDegreeNotFound;
        }

        // Soft delete: flags IsDeleted and keeps the row
        repository.DeleteAsync(entity);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(AcademicDegreeCacheTags.AcademicDegrees, ct);

        return Result.Deleted;
    }
}
