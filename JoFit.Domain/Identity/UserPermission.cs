using JoFit.Domain.Identity.Permissions;
using JoFit.Domain.Identity.Users;

namespace JoFit.Domain.Identity;

public class UserPermission
{
    public long UserId { get; set; }
    public User? User { get; set; }
    public long PermissionId { get; set; }
    public Permission? Permission { get; set; }
    public bool IsGranted { get; set; }
}
