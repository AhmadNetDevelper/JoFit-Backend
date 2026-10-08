using JoFit.Application.Features.Roles.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Roles.Commands.UpdateRole;

public sealed record UpdateRoleCommand(long RoleId, string Name) : IRequest<Result<RoleDto>>;
