namespace JoFit.Domain.Identity.Users;

/// <summary>
/// Defines the functional type of a user within the system.
/// Determines which module a user belongs to and their base access level.
/// </summary>
public enum UserType
{
    /// <summary>
    /// System-wide administrator. Has full access across all modules.
    /// </summary>
    SystemAdmin = 0,

    /// <summary>
    /// Staff member of the vehicle JoFit company (e.g., inspector, receptionist, auditor).
    /// Has access to the JoFit module features based on their assigned roles.
    /// </summary>
    JoFitStaff = 1,

    /// <summary>
    /// Staff member of a bank/financial institution tenant.
    /// Has access only to Bank module features (booking, reports) based on their assigned roles.
    /// </summary>
    BankOfficer = 2
}
