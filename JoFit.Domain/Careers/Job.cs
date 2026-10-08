using System.Text.Json;
using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Careers;

public sealed record SalaryInput(long? Min, long? Max, string Period);

/// <summary>A job as an employer writes it. The text stays in the employer's own language.</summary>
public sealed record JobDetails(
    string Title,
    long CategoryId,
    long LocationId,
    string EmploymentType,
    string WorkMode,
    string ExperienceLevel,
    SalaryInput? Salary,
    bool ShowSalary,
    string Description,
    IReadOnlyList<string> Requirements,
    IReadOnlyList<string> Benefits,
    IReadOnlyList<string> Languages,
    int Positions,
    DateOnly? ClosesOn);

public class Job : AuditableEntity
{
    public long EmployerId { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public long CategoryId { get; private set; }
    public long LocationId { get; private set; }
    public string EmploymentType { get; private set; } = string.Empty;
    public string WorkMode { get; private set; } = string.Empty;
    public string ExperienceLevel { get; private set; } = string.Empty;
    public long? SalaryMinAmount { get; private set; }
    public long? SalaryMaxAmount { get; private set; }
    public string? SalaryPeriod { get; private set; }
    public bool ShowSalary { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string RequirementsJson { get; private set; } = "[]";
    public string BenefitsJson { get; private set; } = "[]";
    public string Languages { get; private set; } = "ar";
    public int Positions { get; private set; } = 1;
    public DateOnly? ClosesOn { get; private set; }
    public string Status { get; private set; } = JobStates.Draft;
    public string? ModerationNote { get; private set; }
    public bool Featured { get; private set; }
    public DateTimeOffset? PostedAtUtc { get; private set; }

    private Job()
    {
    }

    /// <summary>JB-00042, derived from the id.</summary>
    public string Reference => $"JB-{Id:D5}";

    public IReadOnlyList<string> Requirements => JsonSerializer.Deserialize<List<string>>(RequirementsJson) ?? [];

    public IReadOnlyList<string> Benefits => JsonSerializer.Deserialize<List<string>>(BenefitsJson) ?? [];

    public IReadOnlyList<string> LanguageList => Languages.Split(',', StringSplitOptions.RemoveEmptyEntries);

    public static Result<Job> Create(long employerId, string slug, JobDetails details, DateOnly today)
    {
        var job = new Job { EmployerId = employerId, Slug = slug };
        var applied = job.Apply(details, today);
        return applied.IsError ? applied.Errors : job;
    }

    /// <summary>
    /// Cleans and validates a draft (jobDraftErrors): whitespace collapsed, list lines trimmed and empties dropped before
    /// the count, duplicates removed, an empty closing date becomes absent, a salary with no bound is dropped.
    /// </summary>
    public Result<Updated> Apply(JobDetails d, DateOnly today)
    {
        var errors = new List<Error>();
        var title = Employer.Collapse(d.Title);
        var description = (d.Description ?? string.Empty).Trim();
        var requirements = Lines(d.Requirements);
        var benefits = Lines(d.Benefits);
        var languages = (d.Languages ?? []).Select(l => l.Trim().ToLowerInvariant()).Where(l => l.Length > 0).Distinct().ToList();

        if (title.Length is < 5 or > 120) errors.Add(CareersErrors.Field("title", "The title needs 5 to 120 characters."));
        if (d.CategoryId <= 0) errors.Add(CareersErrors.Field("categoryId", "Choose a category."));
        if (d.LocationId <= 0) errors.Add(CareersErrors.Field("cityId", "Choose a city."));
        if (!CareersRules.WorkModes.Contains(d.WorkMode)) errors.Add(CareersErrors.Field("workMode", "Choose a work mode."));
        if (!CareersRules.EmploymentTypes.Contains(d.EmploymentType)) errors.Add(CareersErrors.Field("employmentType", "Choose an employment type."));
        if (!CareersRules.ExperienceLevels.Contains(d.ExperienceLevel)) errors.Add(CareersErrors.Field("experienceLevel", "Choose an experience level."));
        if (description.Length is < 50 or > 5000) errors.Add(CareersErrors.Field("description", "The description needs 50 to 5000 characters."));
        if (requirements.Count > 15 || requirements.Any(r => r.Length is < 2 or > 200)) errors.Add(CareersErrors.Field("requirements", "Up to 15 requirements, each 2 to 200 characters."));
        if (benefits.Count > 15 || benefits.Any(r => r.Length is < 2 or > 200)) errors.Add(CareersErrors.Field("benefits", "Up to 15 benefits, each 2 to 200 characters."));
        if (languages.Count == 0 || languages.Any(l => !CareersRules.JobLanguages.Contains(l))) errors.Add(CareersErrors.Field("languages", "Choose at least one language (ar or en)."));
        if (d.Positions is < 1 or > 50) errors.Add(CareersErrors.Field("positions", "Positions must be from 1 to 50."));
        if (d.ClosesOn is DateOnly closes && closes < today) errors.Add(CareersErrors.Field("closesOn", "The closing date cannot be in the past."));

        // A salary with neither bound is dropped; with a bound it must be sane.
        var salary = d.Salary is { Min: null, Max: null } ? null : d.Salary;
        if (salary is not null)
        {
            if (!CareersRules.SalaryPeriods.Contains(salary.Period)) errors.Add(CareersErrors.Field("salary.period", "Choose per month or per hour."));
            if (salary.Min is <= 0) errors.Add(CareersErrors.Field("salary.min", "The minimum must be more than 0."));
            if (salary.Max is <= 0) errors.Add(CareersErrors.Field("salary.max", "The maximum must be more than 0."));
            if (salary.Min is long min && salary.Max is long max && min > max) errors.Add(CareersErrors.Field("salary.min", "The minimum cannot be above the maximum."));
        }

        if (d.ShowSalary && salary is null) errors.Add(CareersErrors.Field("showSalary", "Add a salary before showing it."));
        if (errors.Count > 0)
        {
            return errors;
        }

        Title = title;
        CategoryId = d.CategoryId;
        LocationId = d.LocationId;
        EmploymentType = d.EmploymentType;
        WorkMode = d.WorkMode;
        ExperienceLevel = d.ExperienceLevel;
        SalaryMinAmount = salary?.Min;
        SalaryMaxAmount = salary?.Max;
        SalaryPeriod = salary?.Period;
        ShowSalary = d.ShowSalary;
        Description = description;
        RequirementsJson = JsonSerializer.Serialize(requirements);
        BenefitsJson = JsonSerializer.Serialize(benefits);
        Languages = string.Join(',', languages);
        Positions = d.Positions;
        ClosesOn = d.ClosesOn;
        return Result.Updated;
    }

    private static List<string> Lines(IReadOnlyList<string>? lines)
        => (lines ?? []).Select(l => (l ?? string.Empty).Trim()).Where(l => l.Length > 0).ToList();

    /// <summary>
    /// The one public test: the job is open, its employer is verified, and its closing date is absent or today or later (Amman).
    /// </summary>
    public bool IsPublic(bool employerVerified, DateOnly today)
        => Status == JobStates.Open && employerVerified && (ClosesOn is null || ClosesOn >= today);

    // ---- lifecycle ------------------------------------------------------------------

    /// <summary>The employer's job was edited: an open job goes back to review, a pending or closed one keeps its status.</summary>
    public bool NeedsReviewAfterEdit => Status == JobStates.Open;

    public void SendBackToReview() => Status = JobStates.Pending;

    public Result<Updated> Submit()
    {
        if (Status is not (JobStates.Draft or JobStates.Rejected))
        {
            return CareersErrors.IllegalJobMove(Status, JobStates.Pending);
        }

        Status = JobStates.Pending;
        ModerationNote = null;
        return Result.Updated;
    }

    public Result<Updated> Withdraw()
    {
        if (Status != JobStates.Pending)
        {
            return CareersErrors.IllegalJobMove(Status, JobStates.Draft);
        }

        Status = JobStates.Draft;
        return Result.Updated;
    }

    public Result<Updated> Close()
    {
        if (Status != JobStates.Open)
        {
            return CareersErrors.IllegalJobMove(Status, JobStates.Closed);
        }

        Status = JobStates.Closed;
        return Result.Updated;
    }

    /// <summary>A closed job can be put back in review, unless its closing date has passed.</summary>
    public Result<Updated> Reopen(DateOnly today)
    {
        if (Status != JobStates.Closed || ClosesOn is DateOnly closes && closes < today)
        {
            return CareersErrors.IllegalJobMove(Status, JobStates.Pending);
        }

        Status = JobStates.Pending;
        return Result.Updated;
    }

    /// <summary>Staff approve a pending job. The employer must be verified; the first approval sets the posting date.</summary>
    public Result<Updated> Approve(bool employerVerified)
    {
        if (Status != JobStates.Pending || !employerVerified)
        {
            return employerVerified ? CareersErrors.IllegalJobMove(Status, JobStates.Open) : CareersErrors.EmployerNotVerified;
        }

        Status = JobStates.Open;
        ModerationNote = null;
        PostedAtUtc ??= DateTimeOffset.UtcNow;
        return Result.Updated;
    }

    public Result<Updated> Reject(string? note)
    {
        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (trimmed is null)
        {
            return CareersErrors.Field("note", "Say why, so the employer can fix it.");
        }

        if (trimmed.Length > 1000)
        {
            return CareersErrors.Field("note", "The note can be at most 1000 characters.");
        }

        if (Status != JobStates.Pending)
        {
            return CareersErrors.IllegalJobMove(Status, JobStates.Rejected);
        }

        Status = JobStates.Rejected;
        ModerationNote = trimmed;
        return Result.Updated;
    }

    /// <summary>Only an open job can be featured; any job can be un-featured.</summary>
    public Result<Updated> SetFeatured(bool featured)
    {
        if (featured && Status != JobStates.Open)
        {
            return CareersErrors.Conflict("Only an open job can be featured.");
        }

        Featured = featured;
        return Result.Updated;
    }
}

public class JobSportLink
{
    public long JobId { get; set; }
    public long SportId { get; set; }
}
