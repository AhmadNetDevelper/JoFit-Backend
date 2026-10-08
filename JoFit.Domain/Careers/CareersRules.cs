namespace JoFit.Domain.Careers;

/// <summary>The fixed lists and limits of the careers marketplace (BACKEND-BRIEF section 7.14 and 12.6).</summary>
public static class CareersRules
{
    public static readonly string[] EmployerKinds = ["gym", "sports-centre", "club", "academy", "clinic", "sports-company", "other"];
    public static readonly string[] EmployerSizes = ["1-10", "11-50", "51-200", "200+"];
    public static readonly string[] EmploymentTypes = ["full-time", "part-time", "contract", "freelance", "internship"];
    public static readonly string[] WorkModes = ["on-site", "hybrid", "remote"];
    public static readonly string[] ExperienceLevels = ["entry", "mid", "senior"];
    public static readonly string[] JobLanguages = ["ar", "en"];
    public static readonly string[] SalaryPeriods = ["month", "hour"];

    /// <summary>Hourly pay is compared as if worked 160 hours a month.</summary>
    public const int HoursPerMonth = 160;
}

public static class EmployerVerification
{
    public const string Pending = "pending";
    public const string Verified = "verified";
    public const string Rejected = "rejected";
    public const string Suspended = "suspended";

    public static readonly string[] All = [Pending, Verified, Rejected, Suspended];

    /// <summary>pending to verified or rejected, verified to suspended, rejected or suspended back to verified.</summary>
    public static bool CanMove(string from, string to) => (from, to) switch
    {
        (Pending, Verified) or (Pending, Rejected) => true,
        (Verified, Suspended) => true,
        (Rejected, Verified) or (Suspended, Verified) => true,
        _ => false
    };
}

public static class JobStates
{
    public const string Draft = "draft";
    public const string Pending = "pending";
    public const string Open = "open";
    public const string Closed = "closed";
    public const string Rejected = "rejected";

    public static readonly string[] All = [Draft, Pending, Open, Closed, Rejected];
}

public static class ApplicationStatuses
{
    public const string Submitted = "submitted";
    public const string Reviewing = "reviewing";
    public const string Shortlisted = "shortlisted";
    public const string Interview = "interview";
    public const string Offered = "offered";
    public const string Hired = "hired";
    public const string Rejected = "rejected";
    public const string Withdrawn = "withdrawn";

    public static readonly string[] All = [Submitted, Reviewing, Shortlisted, Interview, Offered, Hired, Rejected, Withdrawn];

    /// <summary>Statuses that count as a live application (the candidate cannot apply again).</summary>
    public static readonly string[] NotWithdrawn = [Submitted, Reviewing, Shortlisted, Interview, Offered, Hired, Rejected];

    /// <summary>The employer's moves: one step forward, or rejected at any point before hired. Hired, rejected and withdrawn are final.</summary>
    public static IReadOnlyList<string> EmployerMoves(string from) => from switch
    {
        Submitted => [Reviewing, Rejected],
        Reviewing => [Shortlisted, Rejected],
        Shortlisted => [Interview, Rejected],
        Interview => [Offered, Rejected],
        Offered => [Hired, Rejected],
        _ => []
    };

    /// <summary>A candidate may withdraw until the employer has decided.</summary>
    public static bool CanWithdraw(string status) => status is Submitted or Reviewing or Shortlisted or Interview or Offered;
}
