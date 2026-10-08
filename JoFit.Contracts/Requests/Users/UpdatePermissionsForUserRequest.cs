namespace JoFit.Contracts.Requests.Users;

public sealed record UpdatePermissionsForUserRequest
{
    public List<UserPermissionRequest> Permissions { get; init; } = [];
}

/// <param name="PermissionId">The permission to grant or revoke.</param>
/// <param name="IsGranted">True to grant the permission to the user, false to revoke it.</param>
public sealed record UserPermissionRequest(long PermissionId, bool IsGranted);
