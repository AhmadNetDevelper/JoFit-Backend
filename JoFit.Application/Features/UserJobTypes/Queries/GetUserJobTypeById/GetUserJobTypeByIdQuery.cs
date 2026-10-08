using JoFit.Application.Features.UserJobTypes.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.UserJobTypes.Queries.GetUserJobTypeById;

public sealed record GetUserJobTypeByIdQuery(long Id) : IRequest<Result<UserJobTypeDto>>;
