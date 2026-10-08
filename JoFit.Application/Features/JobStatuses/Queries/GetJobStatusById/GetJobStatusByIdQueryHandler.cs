using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.JobStatuses.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.JobStatuses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.JobStatuses.Queries.GetJobStatusById;

public class GetJobStatusByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetJobStatusByIdQuery, Result<JobStatusDto>>
{
    public async Task<Result<JobStatusDto>> Handle(GetJobStatusByIdQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<JobStatus>()
            .GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return JobStatusErrors.JobStatusNotFound;
        }

        return JobStatusDto.From(entity);
    }
}
