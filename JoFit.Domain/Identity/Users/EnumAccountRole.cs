namespace JoFit.Domain.Identity.Users;

/// <summary>The storefront role of an account, carried in the session and the access token.</summary>
public enum EnumAccountRole
{
    Customer = 0,
    Coach = 1,
    Employer = 2
}

public static class EnumAccountRoleExtensions
{
    /// <summary>The role name as it appears in claims and in the API ("customer", "coach", "employer").</summary>
    public static string ToRoleName(this EnumAccountRole role) => role.ToString().ToLowerInvariant();
}
