using Microsoft.AspNetCore.Authorization;

namespace JoFit.API.Authorization
{
    /// <summary>
    /// Represents a permission requirement for authorization.
    /// This allows the authorization system to check if a user has the required permission.
    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Gets the permission name (e.g., "colors.read", "users.create").
        /// </summary>
        public string Permission { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionRequirement"/> class.
        /// </summary>
        /// <param name="permission">The permission name in format "resource.action" (e.g., "colors.read")</param>
        public PermissionRequirement(string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
            {
                throw new ArgumentException($"'{nameof(permission)}' cannot be null or whitespace.", nameof(permission));
            }

            Permission = permission;
        }
    }
}
