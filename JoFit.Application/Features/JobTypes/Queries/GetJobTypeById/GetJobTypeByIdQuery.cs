using JoFit.Application.Features.JobTypes.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.JobTypes.Queries.GetJobTypeById;

public sealed record GetJobTypeByIdQuery(long Id) : IRequest<Result<JobTypeDto>>;
