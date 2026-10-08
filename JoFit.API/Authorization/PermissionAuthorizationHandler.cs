using JoFit.Application.Common.Interfaces;
using JoFit.Domain.Identity;
using JoFit.Domain.Identity.Roles;
using JoFit.Domain.Identity.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JoFit.API.Authorization
{
    /// <summary>
    /// Authorization handler for checking if a user has the required permission.
    /// This handler evaluates PermissionRequirement by checking the user's roles
    /// and direct user permissions from the database tables (RolePermissions and UserPermissions).
    /// Explicit revocation (IsGranted = false) takes precedence over role-based permissions.
    /// </summary>
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private const string PermissionPrefix = "Permissions.";
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionAuthorizationHandler"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving scoped services</param>
        public PermissionAuthorizationHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Handles the authorization request by checking if the user has the required permission
        /// from either direct user permissions or role-based permissions.
        /// Explicit permission revocation (IsGranted = false) always denies access.
        /// </summary>
        /// <param name="context">The authorization handler context</param>
        /// <param name="requirement">The permission requirement to check</param>
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // Check if the user is authenticated and has an identity
            if (context.User == null)
            {
                context.Fail();
                return;
            }

            // Use a scope to get scoped services
            using (var scope = _serviceProvider.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                // Get the user from the context
                var user = await userManager.GetUserAsync(context.User);

                if (user == null)
                {
                    context.Fail();
                    return;
                }

                try
                {
                    var userPermissionRepository = unitOfWork.GetRepository<UserPermission>();
                    var rolePermissionRepository = unitOfWork.GetRepository<RolePermission>();

                    // Normalize the required permission for comparison
                    var normalizedRequiredPermission = NormalizePermissionName(requirement.Permission);

                    // Get all direct user permissions (both granted and revoked)
                    var allUserPermissions = await userPermissionRepository.GetTableNoTracking()
                        .Where(up => up.UserId == user.Id)
                        .Include(up => up.Permission)
                        .ToListAsync();

                    // Check if the permission is explicitly revoked (IsGranted = false)
                    var revokedPermission = allUserPermissions
                        .FirstOrDefault(up => 
                            !up.IsGranted && 
                            NormalizePermissionName(up.Permission.Name) == normalizedRequiredPermission);

                    if (revokedPermission != null)
                    {
                        // Permission is explicitly revoked - deny access regardless of role permissions
                        context.Fail();
                        return;
                    }

                    // Check if permission is explicitly granted to the user
                    var grantedUserPermission = allUserPermissions
                        .FirstOrDefault(up => 
                            up.IsGranted && 
                            NormalizePermissionName(up.Permission.Name) == normalizedRequiredPermission);

                    if (grantedUserPermission != null)
                    {
                        context.Succeed(requirement);
                        return;
                    }

                    // Get role-based permissions
                    // First, get all roles assigned to the user
                    var dbContext = unitOfWork.GetDbContext();
                    var userRoles = await dbContext.Set<IdentityUserRole<long>>()
                        .Where(ur => ur.UserId == user.Id)
                        .Select(ur => ur.RoleId)
                        .ToListAsync();

                    if (userRoles.Any())
                    {
                        // Get permissions for those roles
                        var rolePermissions = await rolePermissionRepository.GetTableNoTracking()
                            .Where(rp => userRoles.Contains(rp.RoleId))
                            .Include(rp => rp.Permission)
                            .Select(rp => rp.Permission.Name)
                            .Distinct()
                            .ToListAsync();

                        // Normalize role permission names for comparison
                        var normalizedRolePermissions = rolePermissions
                            .Select(NormalizePermissionName)
                            .ToList();

                        // Check if permission is in role-based permissions
                        if (normalizedRolePermissions.Contains(normalizedRequiredPermission))
                        {
                            context.Succeed(requirement);
                            return;
                        }
                    }

                    // If no permission was found, fail the authorization
                    context.Fail();
                }
                catch
                {
                    // If any error occurs, fail the authorization for security
                    context.Fail();
                }
            }
        }

        /// <summary>
        /// Normalizes permission names to lowercase dotted format without the "Permissions." prefix.
        /// This matches the format used by PermissionPolicyProvider.
        /// Example: "Permissions.Colors.View" ? "colors.view"
        /// </summary>
        /// <param name="permissionName">The permission name to normalize</param>
        /// <returns>The normalized permission name in lowercase dotted format without prefix</returns>
        private static string NormalizePermissionName(string permissionName)
        {
            if (string.IsNullOrWhiteSpace(permissionName))
                return string.Empty;

            // Convert to lowercase
            var lowercase = permissionName.ToLowerInvariant();

            // Remove "permissions." prefix if present
            if (lowercase.StartsWith(PermissionPrefix.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase))
            {
                lowercase = lowercase.Substring(PermissionPrefix.Length);
            }

            return lowercase;
        }
    }
}
