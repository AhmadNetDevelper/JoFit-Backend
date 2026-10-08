namespace JoFit.Domain.Identity.Roles;

using JoFit.Domain.Common.Results;

public static class RoleErrors
{
    public static Error RoleExists => Error.Conflict(
        code: "Role.Exists",
        description: "A role with the same name already exists."
    );

    public static Error RoleNotFound => Error.NotFound(
        code: "Role.NotFound",
        description: "The specified role was not found."
    );
}
