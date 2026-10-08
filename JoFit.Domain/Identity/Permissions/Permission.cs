using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Identity.Permissions;

public class Permission : Entity
{
    public string Name { get; private set; } // e.g., "users.create", "reports.view"
    public string Resource { get; private set; } // e.g., "users", "reports"
    public string Action { get; private set; } // e.g., "create", "view", "edit", "delete"
    public DateTime CreatedAt { get; private set; }

    public ICollection<RolePermission> RolePermissions { get; } = [];
    public ICollection<UserPermission> UserPermissions { get; } = [];

    private Permission()
    {
    }

    private Permission(string name, string resource, string action) : base()
    {
        Name = name;
        Resource = resource;
        Action = action;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new Permission with validation.
    /// </summary>
    /// <param name="name">The permission name (e.g., "users:create")</param>
    /// <param name="resource">The permission resource (e.g., "users")</param>
    /// <param name="action">The permission action (e.g., "create")</param>
    /// <returns>A Result containing the created Permission or validation errors</returns>
    public static Result<Permission> Create(string name, string resource, string action)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return PermissionErrors.NameRequired;
        }

        if (string.IsNullOrWhiteSpace(resource))
        {
            return PermissionErrors.ResourceRequired;
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            return PermissionErrors.ActionRequired;
        }

        return new Permission(name, resource, action);
    }
}
