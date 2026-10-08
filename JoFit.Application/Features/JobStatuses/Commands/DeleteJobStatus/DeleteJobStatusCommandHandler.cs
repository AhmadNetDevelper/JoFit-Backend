using JoFit.Application.Common.Interfaces;
using JoFit.Domain.Common.Results;
using JoFit.Domain.JobStatuses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.JobStatuses.Commands.DeleteJobStatus;

public class DeleteJobStatusCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<DeleteJobStatusCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteJobStatusCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<JobStatus>();

        var entity = await repository.GetTableAsTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return JobStatusErrors.JobStatusNotFound;
        }

        // Soft delete: flags IsDeleted and keeps the row
        repository.DeleteAsync(entity);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(JobStatusCacheTags.JobStatuses, ct);

        return Result.Deleted;
    }
}
