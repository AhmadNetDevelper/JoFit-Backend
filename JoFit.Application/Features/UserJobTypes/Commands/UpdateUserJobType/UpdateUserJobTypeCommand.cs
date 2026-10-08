using JoFit.Application.Features.UserJobTypes.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.UserJobTypes.Commands.UpdateUserJobType;

public sealed record UpdateUserJobTypeCommand(
    long Id,
    string NameAr,
    string NameEn,
    bool IsActive) : IRequest<Result<UserJobTypeDto>>;
