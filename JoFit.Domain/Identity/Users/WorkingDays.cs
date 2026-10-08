namespace JoFit.Domain.Identity.Users;

/// <summary>
/// Bit-flag enum representing the days of the week a user works.
/// Multiple days can be combined: e.g., Saturday | Sunday | Monday.
/// </summary>
[Flags]
public enum WorkingDays
{
    None      = 0,
    Saturday  = 1 << 0,  // 1
    Sunday    = 1 << 1,  // 2
    Monday    = 1 << 2,  // 4
    Tuesday   = 1 << 3,  // 8
    Wednesday = 1 << 4,  // 16
    Thursday  = 1 << 5,  // 32
    Friday    = 1 << 6,  // 64

    /// <summary>Saturday through Thursday (standard work week).</summary>
    WeekDays = Saturday | Sunday | Monday | Tuesday | Wednesday | Thursday,

    /// <summary>All seven days.</summary>
    AllDays = Saturday | Sunday | Monday | Tuesday | Wednesday | Thursday | Friday
}
