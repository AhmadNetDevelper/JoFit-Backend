using JoFit.Application.Features.Permissions.Dtos;
using JoFit.Application.Features.Roles.Dtos;
using JoFit.Domain.Identity.Permissions;
using JoFit.Domain.Identity.Roles;

namespace JoFit.Application.Features.Roles.Mappers;

public static class RoleMapper
{
    public static RoleDto ToDto(this Role entity)
        => new(entity.Id, entity.Name!);

    public static List<RoleDto> ToDtos(this IEnumerable<Role> entities)
        => [.. entities.Select(e => e.ToDto())];

    public static RolePermissionDto ToRolePermissionDto(this Role role, IEnumerable<Permission> permissions)
        => new(
            role.Id,
            role.Name!,
            [.. permissions.Select(p => new PermissionDto(p.Id, p.Name, p.Resource, p.Action))]);
}
