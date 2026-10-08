using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.JobStatuses.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.JobStatuses;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.JobStatuses.Commands.CreateJobStatus;

public class CreateJobStatusCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<CreateJobStatusCommand, Result<JobStatusDto>>
{
    public async Task<Result<JobStatusDto>> Handle(CreateJobStatusCommand request, CancellationToken ct)
    {
        var createResult = JobStatus.Create(request.NameAr, request.NameEn, request.IsActive);
        if (createResult.IsError)
        {
            return createResult.Errors;
        }

        var entity = createResult.Value;

        await unitOfWork.GetRepository<JobStatus>().AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(JobStatusCacheTags.JobStatuses, ct);

        return JobStatusDto.From(entity);
    }
}
