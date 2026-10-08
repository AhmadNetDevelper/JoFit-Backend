namespace JoFit.Application.Features.Users.Dtos;

public sealed record UserPermissionDto(
    long UserId,
    long PermissionId,
    string PermissionName,
    string Resource,
    string Action,
    bool IsGranted);
