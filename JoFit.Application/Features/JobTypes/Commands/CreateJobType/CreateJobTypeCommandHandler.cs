using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.JobTypes.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.JobTypes;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.JobTypes.Commands.CreateJobType;

public class CreateJobTypeCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<CreateJobTypeCommand, Result<JobTypeDto>>
{
    public async Task<Result<JobTypeDto>> Handle(CreateJobTypeCommand request, CancellationToken ct)
    {
        var createResult = JobType.Create(request.NameAr, request.NameEn, request.IsActive);
        if (createResult.IsError)
        {
            return createResult.Errors;
        }

        var entity = createResult.Value;

        await unitOfWork.GetRepository<JobType>().AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(JobTypeCacheTags.JobTypes, ct);

        return JobTypeDto.From(entity);
    }
}
