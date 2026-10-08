using JoFit.Application.Features.JobStatuses.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.JobStatuses.Commands.UpdateJobStatus;

public sealed record UpdateJobStatusCommand(
    long Id,
    string NameAr,
    string NameEn,
    bool IsActive) : IRequest<Result<JobStatusDto>>;
