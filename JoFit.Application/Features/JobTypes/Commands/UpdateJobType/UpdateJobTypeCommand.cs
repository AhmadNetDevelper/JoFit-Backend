using JoFit.Application.Features.JobTypes.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.JobTypes.Commands.UpdateJobType;

public sealed record UpdateJobTypeCommand(
    long Id,
    string NameAr,
    string NameEn,
    bool IsActive) : IRequest<Result<JobTypeDto>>;
