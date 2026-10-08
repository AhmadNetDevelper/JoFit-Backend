namespace JoFit.Application.Features.Permissions.Dtos;

public sealed record PermissionDto(long Id, string Name, string Resource, string Action);
