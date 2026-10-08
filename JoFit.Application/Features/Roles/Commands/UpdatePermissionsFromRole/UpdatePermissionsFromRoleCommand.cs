using JoFit.Application.Features.Roles.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Roles.Commands.UpdatePermissionsFromRole;

public sealed record UpdatePermissionsFromRoleCommand(long RoleId, List<long> PermissionIds)
    : IRequest<Result<RolePermissionDto>>;
