using JoFit.Application.Features.Roles.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Roles.Commands.AssignPermissionsToRole;

public sealed record AssignPermissionsToRoleCommand(long RoleId, List<long> PermissionIds)
    : IRequest<Result<RolePermissionDto>>;
