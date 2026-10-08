using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Identity.Permissions;

public static class PermissionErrors
{
    public static Error NameRequired =>
        Error.Validation("Permission.NameRequired", "Permission name is required.");

    public static Error ResourceRequired =>
        Error.Validation("Permission.ResourceRequired", "Permission resource is required.");

    public static Error ActionRequired =>
        Error.Validation("Permission.ActionRequired", "Permission action is required.");
}
