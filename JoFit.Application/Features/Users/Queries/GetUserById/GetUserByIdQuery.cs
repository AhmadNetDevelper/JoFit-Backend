using JoFit.Application.Features.Users.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Users.Queries.GetUserById;

public sealed record GetUserByIdQuery(long UserId) : IRequest<Result<UserDto>>;
