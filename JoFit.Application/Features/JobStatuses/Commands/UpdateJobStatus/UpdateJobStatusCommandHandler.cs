using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.JobStatuses.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.JobStatuses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.JobStatuses.Commands.UpdateJobStatus;

public class UpdateJobStatusCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<UpdateJobStatusCommand, Result<JobStatusDto>>
{
    public async Task<Result<JobStatusDto>> Handle(UpdateJobStatusCommand request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<JobStatus>()
            .GetTableAsTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return JobStatusErrors.JobStatusNotFound;
        }

        var updateResult = entity.Update(request.NameAr, request.NameEn, request.IsActive);
        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(JobStatusCacheTags.JobStatuses, ct);

        return JobStatusDto.From(entity);
    }
}
