using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Users.Commands.RemoveUser;

public sealed record RemoveUserCommand(long UserId) : IRequest<Result<Deleted>>;
