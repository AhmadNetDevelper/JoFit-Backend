using JoFit.Application.Features.JobStatuses.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.JobStatuses.Commands.CreateJobStatus;

public sealed record CreateJobStatusCommand(
    string NameAr,
    string NameEn,
    bool IsActive = true) : IRequest<Result<JobStatusDto>>;
