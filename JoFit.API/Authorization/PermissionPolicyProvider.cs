using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace JoFit.API.Authorization
{
    /// <summary>
    /// Custom authorization policy provider that dynamically creates policies for permissions.
    /// This provider handles permission-based policies while delegating other policies to the default provider.
    /// </summary>
    public class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private const string PolicyPrefix = "Permissions.";
        private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            // Fallback provider for non-permission policies
            _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        /// <summary>
        /// Gets the default policy. Delegates to the fallback provider.
        /// </summary>
        public async Task<AuthorizationPolicy?> GetDefaultPolicyAsync()
        {
            return await _fallbackPolicyProvider.GetDefaultPolicyAsync();
        }

        /// <summary>
        /// Gets a fallback policy. Delegates to the fallback provider.
        /// </summary>
        public async Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        {
            return await _fallbackPolicyProvider.GetFallbackPolicyAsync();
        }

        /// <summary>
        /// Gets or creates a policy dynamically based on the policy name.
        /// If the policy name starts with "Permissions.", creates a permission-based policy.
        /// Otherwise, delegates to the fallback provider.
        /// </summary>
        /// <param name="policyName">The name of the policy to get or create</param>
        /// <returns>An authorization policy or null if the policy name doesn't match</returns>
        public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Check if this is a permission-based policy
            if (policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                // Extract the permission name from the policy name
                // "Permissions.Colors.Read" ? "colors.read"
                var permissionName = policyName.Substring(PolicyPrefix.Length);

                if (string.IsNullOrWhiteSpace(permissionName))
                {
                    return null;
                }

                // Convert PascalCase to lowercase dotted format
                var convertedPermission = ConvertToDottedFormat(permissionName);

                // Dynamically create a policy with the permission requirement
                var policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement(convertedPermission))
                    .Build();

                return policy;
            }

            // For non-permission policies, use the fallback provider
            return await _fallbackPolicyProvider.GetPolicyAsync(policyName);
        }

        /// <summary>
        /// Converts PascalCase format to lowercase dotted format.
        /// Example: "Colors.Read" ? "colors.read"
        /// </summary>
        private static string ConvertToDottedFormat(string input)
        {
            return input.ToLowerInvariant();
        }

        /// <summary>
        /// Gets the policy prefix used for permission-based policies.
        /// </summary>
        public static string GetPolicyPrefix => PolicyPrefix;

        /// <summary>
        /// Creates a policy name from resource and action names.
        /// </summary>
        /// <param name="resource">The resource name (e.g., "Colors")</param>
        /// <param name="action">The action name (e.g., "Read")</param>
        /// <returns>The policy name (e.g., "Permissions.Colors.Read")</returns>
        public static string GetPolicyName(string resource, string action)
        {
            return $"{PolicyPrefix}{resource}.{action}";
        }
    }
}
