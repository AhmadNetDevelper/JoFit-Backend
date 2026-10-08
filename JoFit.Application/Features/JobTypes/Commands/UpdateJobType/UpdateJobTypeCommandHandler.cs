using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.JobTypes.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.JobTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.JobTypes.Commands.UpdateJobType;

public class UpdateJobTypeCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<UpdateJobTypeCommand, Result<JobTypeDto>>
{
    public async Task<Result<JobTypeDto>> Handle(UpdateJobTypeCommand request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<JobType>()
            .GetTableAsTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return JobTypeErrors.JobTypeNotFound;
        }

        var updateResult = entity.Update(request.NameAr, request.NameEn, request.IsActive);
        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(JobTypeCacheTags.JobTypes, ct);

        return JobTypeDto.From(entity);
    }
}
