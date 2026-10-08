namespace JoFit.API.Authorization;

public static class Permissions
{
    public static class Users
    {
        public const string Read = "Permissions.Users.Read";
        public const string View = "Permissions.Users.View";
        public const string Create = "Permissions.Users.Create";
        public const string Update = "Permissions.Users.Update";
        public const string Delete = "Permissions.Users.Delete";
    }

    public static class Roles
    {
        public const string Read = "Permissions.Roles.Read";
        public const string View = "Permissions.Roles.View";
        public const string Create = "Permissions.Roles.Create";
        public const string Update = "Permissions.Roles.Update";
        public const string Delete = "Permissions.Roles.Delete";
    }

    public static class UserJobTypes
    {
        public const string Read = "Permissions.UserJobTypes.Read";
        public const string View = "Permissions.UserJobTypes.View";
        public const string Create = "Permissions.UserJobTypes.Create";
        public const string Update = "Permissions.UserJobTypes.Update";
        public const string Delete = "Permissions.UserJobTypes.Delete";
    }

    public static class Countries
    {
        public const string Read = "Permissions.Countries.Read";
        public const string View = "Permissions.Countries.View";
        public const string Create = "Permissions.Countries.Create";
        public const string Update = "Permissions.Countries.Update";
        public const string Delete = "Permissions.Countries.Delete";
    }

    public static class Cities
    {
        public const string Read = "Permissions.Cities.Read";
        public const string View = "Permissions.Cities.View";
        public const string Create = "Permissions.Cities.Create";
        public const string Update = "Permissions.Cities.Update";
        public const string Delete = "Permissions.Cities.Delete";
    }

    public static class JobStatuses
    {
        public const string Read = "Permissions.JobStatuses.Read";
        public const string View = "Permissions.JobStatuses.View";
        public const string Create = "Permissions.JobStatuses.Create";
        public const string Update = "Permissions.JobStatuses.Update";
        public const string Delete = "Permissions.JobStatuses.Delete";
    }

    public static class AcademicDegrees
    {
        public const string Read = "Permissions.AcademicDegrees.Read";
        public const string View = "Permissions.AcademicDegrees.View";
        public const string Create = "Permissions.AcademicDegrees.Create";
        public const string Update = "Permissions.AcademicDegrees.Update";
        public const string Delete = "Permissions.AcademicDegrees.Delete";
    }

    public static class UniversityMajors
    {
        public const string Read = "Permissions.UniversityMajors.Read";
        public const string View = "Permissions.UniversityMajors.View";
        public const string Create = "Permissions.UniversityMajors.Create";
        public const string Update = "Permissions.UniversityMajors.Update";
        public const string Delete = "Permissions.UniversityMajors.Delete";
    }

    public static class JobTypes
    {
        public const string Read = "Permissions.JobTypes.Read";
        public const string View = "Permissions.JobTypes.View";
        public const string Create = "Permissions.JobTypes.Create";
        public const string Update = "Permissions.JobTypes.Update";
        public const string Delete = "Permissions.JobTypes.Delete";
    }

    public static class Courses
    {
        public const string Read = "Permissions.Courses.Read";
        public const string View = "Permissions.Courses.View";
        public const string Create = "Permissions.Courses.Create";
        public const string Update = "Permissions.Courses.Update";
        public const string Delete = "Permissions.Courses.Delete";
    }

    /// <summary>Products, categories and brands (back-office).</summary>
    public static class Catalogue
    {
        public const string Read = "Permissions.Catalogue.Read";
        public const string Write = "Permissions.Catalogue.Write";
    }

    public static class Rental
    {
        public const string Read = "Permissions.Rental.Read";
        public const string Write = "Permissions.Rental.Write";
    }

    public static class Orders
    {
        public const string Read = "Permissions.Orders.Read";
        public const string Write = "Permissions.Orders.Write";
    }

    public static class Customers
    {
        public const string Read = "Permissions.Customers.Read";
        public const string Write = "Permissions.Customers.Write";
    }

    public static class Coaches
    {
        public const string Read = "Permissions.Coaches.Read";
        public const string Write = "Permissions.Coaches.Write";
    }

    public static class Education
    {
        public const string Read = "Permissions.Education.Read";
        public const string Write = "Permissions.Education.Write";
    }

    public static class Reviews
    {
        public const string Read = "Permissions.Reviews.Read";
        public const string Moderate = "Permissions.Reviews.Moderate";
    }

    public static class Content
    {
        public const string Read = "Permissions.Content.Read";
        public const string Write = "Permissions.Content.Write";
    }

    public static class Quotations
    {
        public const string Read = "Permissions.Quotations.Read";
        public const string Write = "Permissions.Quotations.Write";
    }

    public static class Community
    {
        public const string Moderate = "Permissions.Community.Moderate";
    }

    public static class Support
    {
        public const string Reply = "Permissions.Support.Reply";
    }

    public static class Careers
    {
        public const string Read = "Permissions.Careers.Read";
        public const string Write = "Permissions.Careers.Write";
    }

    public static class Sports
    {
        public const string Write = "Permissions.Sports.Write";
    }

    public static class Settings
    {
        public const string Write = "Permissions.Settings.Write";
    }

    public static class PermissionsManagement
    {
        public const string Read = "Permissions.Permissions.Read";
        public const string View = "Permissions.Permissions.View";
        public const string Create = "Permissions.Permissions.Create";
        public const string Update = "Permissions.Permissions.Update";
        public const string Delete = "Permissions.Permissions.Delete";
    }

    /// <summary>
    /// Gets all permissions with their resource and action.
    /// Used by the database seeder (via reflection) to sync the Permissions table.
    /// </summary>
    /// <returns>All permissions with resource and action information</returns>
    public static IEnumerable<(string Resource, string Action, string Name)> GetAllWithDetails()
    {
        var nestedTypes = typeof(Permissions).GetNestedTypes();

        foreach (var type in nestedTypes)
        {
            // Extract resource name from the nested class name
            var resourceName = type.Name;
            if (resourceName == "PermissionsManagement")
                resourceName = "Permissions";

            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(field => field.FieldType == typeof(string))
                .ToList();

            foreach (var field in fields)
            {
                var permissionName = field.GetValue(null)?.ToString();
                if (!string.IsNullOrEmpty(permissionName))
                {
                    // Extract action from the permission name (e.g., "Permissions.Users.Read" -> "Read")
                    var parts = permissionName.Split('.');
                    var action = parts.Length > 0 ? parts[^1] : string.Empty;

                    yield return (resourceName, action, permissionName);
                }
            }
        }
    }
}
