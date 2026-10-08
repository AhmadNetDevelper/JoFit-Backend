using JoFit.Domain.Identity.Permissions;
using JoFit.Domain.Identity.Roles;

namespace JoFit.Domain.Identity;

public class RolePermission
{
    public long RoleId { get; set; }
    public Role Role { get; set; }

    public long PermissionId { get; set; }
    public Permission Permission { get; set; }
}
