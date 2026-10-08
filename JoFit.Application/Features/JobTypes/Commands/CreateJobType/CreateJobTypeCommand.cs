using JoFit.Application.Features.JobTypes.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.JobTypes.Commands.CreateJobType;

public sealed record CreateJobTypeCommand(
    string NameAr,
    string NameEn,
    bool IsActive = true) : IRequest<Result<JobTypeDto>>;
