using JoFit.Application.Features.UserJobTypes.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.UserJobTypes.Commands.CreateUserJobType;

public sealed record CreateUserJobTypeCommand(
    string NameAr,
    string NameEn,
    bool IsActive = true) : IRequest<Result<UserJobTypeDto>>;
