using JoFit.Application.Features.JobStatuses.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.JobStatuses.Queries.GetJobStatusById;

public sealed record GetJobStatusByIdQuery(long Id) : IRequest<Result<JobStatusDto>>;
