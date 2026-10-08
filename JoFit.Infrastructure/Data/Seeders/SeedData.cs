using JoFit.Domain.Identity.Roles;

namespace JoFit.Infrastructure.Data.Seeders;

/// <summary>
/// Contains constants for seeding initial data into the database.
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Default SuperAdmin user credentials. Change these in production!
    /// </summary>
    public static class SuperAdmin
    {
        public const string UserName = "superadmin";
        public const string Email = "superadmin@jofit.com";
        public const string Password = "SuperAdmin@123"; // Change in production!
        public const string PhoneNumber = "123456789";
    }

    /// <summary>
    /// Default role names
    /// </summary>
    public static class Roles
    {
        public const string SuperAdmin = Role.SuperAdmin;
        public const string Coach = "coach";
        public const string User = "user";

        public static readonly string[] All =
        [
            SuperAdmin,
            Coach,
            User
        ];
    }

    /// <summary>
    /// Permission definitions - uses reflection to get permissions from JoFit.API.Authorization.Permissions
    /// </summary>
    public static class Permissions
    {
        /// <summary>
        /// Gets all permissions defined in the system.
        /// Permissions are sourced from the JoFit.API.Authorization.Permissions class using reflection.
        /// </summary>
        public static IEnumerable<(string Resource, string Action, string Name)> GetAll()
        {
            try
            {
                // Load the JoFit.API assembly first
                var assembly = System.Reflection.Assembly.Load("JoFit.API");
                if (assembly == null)
                {
                    throw new InvalidOperationException("Could not load JoFit.API assembly");
                }

                // Get the Permissions type from the loaded assembly
                var permissionsType = assembly.GetType("JoFit.API.Authorization.Permissions");
                if (permissionsType == null)
                {
                    throw new InvalidOperationException("Could not find JoFit.API.Authorization.Permissions class in assembly");
                }

                // Get the GetAllWithDetails method
                var method = permissionsType.GetMethod("GetAllWithDetails", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method == null)
                {
                    throw new InvalidOperationException("Could not find GetAllWithDetails method in Permissions class");
                }

                // Invoke the method
                var result = method.Invoke(null, null);
                if (result is IEnumerable<(string, string, string)> permissions)
                {
                    return permissions;
                }

                throw new InvalidOperationException("GetAllWithDetails did not return expected type");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving permissions from JoFit.API.Authorization.Permissions: {ex.Message}");
                throw;
            }
        }
    }
}
