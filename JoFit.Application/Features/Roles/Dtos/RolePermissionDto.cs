using JoFit.Application.Features.Permissions.Dtos;

namespace JoFit.Application.Features.Roles.Dtos;

public sealed record RolePermissionDto(long RoleId, string Name, List<PermissionDto> Permissions);
