using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Roles.Commands.RemoveRole;

public sealed record RemoveRoleCommand(long RoleId) : IRequest<Result<Deleted>>;
