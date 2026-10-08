using JoFit.Application.Common.Interfaces;
using JoFit.Domain.Common.Results;
using JoFit.Domain.JobTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.JobTypes.Commands.DeleteJobType;

public class DeleteJobTypeCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<DeleteJobTypeCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteJobTypeCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<JobType>();

        var entity = await repository.GetTableAsTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return JobTypeErrors.JobTypeNotFound;
        }

        // Soft delete: flags IsDeleted and keeps the row
        repository.DeleteAsync(entity);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(JobTypeCacheTags.JobTypes, ct);

        return Result.Deleted;
    }
}
