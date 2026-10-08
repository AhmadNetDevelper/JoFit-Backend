namespace JoFit.Contracts.Requests.Roles;

public sealed record UpdatePermissionsFromRoleRequest
{
    public List<long> PermissionIds { get; init; } = [];
}
