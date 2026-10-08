using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.JobTypes.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.JobTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.JobTypes.Queries.GetJobTypeById;

public class GetJobTypeByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetJobTypeByIdQuery, Result<JobTypeDto>>
{
    public async Task<Result<JobTypeDto>> Handle(GetJobTypeByIdQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<JobType>()
            .GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return JobTypeErrors.JobTypeNotFound;
        }

        return JobTypeDto.From(entity);
    }
}
