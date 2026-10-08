namespace JoFit.Contracts.Requests.Roles;

public sealed record AssignPermissionsToRoleRequest
{
    public List<long> PermissionIds { get; init; } = [];
}
