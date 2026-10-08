using System.Text.Json;
using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Careers;

/// <summary>A candidate's CV. It is private: served only through the owner, the employer applied to, and staff.</summary>
public class CvFile : AuditableEntity
{
    public long CustomerId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public long Bytes { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;

    private CvFile()
    {
    }

    public static CvFile Create(long customerId, string fileName, string mimeType, long bytes, string storageKey)
        => new() { CustomerId = customerId, FileName = fileName.Length > 120 ? fileName[..120] : fileName, MimeType = mimeType, Bytes = bytes, StorageKey = storageKey };
}

public sealed record ApplicationDraft(
    long JobId,
    long CvId,
    string CoverLetter,
    int YearsExperience,
    string? Certifications,
    DateOnly? AvailableFrom,
    long? ExpectedSalaryAmount);

public sealed record HistoryEntry(string Status, DateTimeOffset At, string By);

public class JobApplication : AuditableEntity
{
    public long JobId { get; private set; }
    public long CustomerId { get; private set; }

    // The candidate as they were when they applied.
    public string CandidateName { get; private set; } = string.Empty;
    public string CandidateEmail { get; private set; } = string.Empty;
    public string? CandidatePhone { get; private set; }

    public long CvFileId { get; private set; }
    public string CoverLetter { get; private set; } = string.Empty;
    public int YearsExperience { get; private set; }
    public string? Certifications { get; private set; }
    public DateOnly? AvailableFrom { get; private set; }
    public long? ExpectedSalaryAmount { get; private set; }
    public string Status { get; private set; } = ApplicationStatuses.Submitted;

    /// <summary>What the employer told the candidate with the last move. The candidate sees it.</summary>
    public string? StatusNote { get; private set; }

    /// <summary>Why the candidate withdrew. Kept apart from the status note.</summary>
    public string? WithdrawReason { get; private set; }

    /// <summary>The employer's private note. Never in a candidate's or staff member's payload.</summary>
    public string? EmployerNote { get; private set; }
    public string HistoryJson { get; private set; } = "[]";
    public DateTimeOffset? SeenByEmployerAtUtc { get; private set; }
    public DateTimeOffset SubmittedAtUtc { get; private set; }

    private JobApplication()
    {
    }

    /// <summary>JA-00042, derived from the id.</summary>
    public string Reference => $"JA-{Id:D5}";

    public IReadOnlyList<HistoryEntry> History => JsonSerializer.Deserialize<List<HistoryEntry>>(HistoryJson) ?? [];

    public bool IsNew => Status == ApplicationStatuses.Submitted && SeenByEmployerAtUtc is null;

    public static Result<JobApplication> Submit(
        long customerId, string candidateName, string candidateEmail, string? candidatePhone, ApplicationDraft d, DateOnly today)
    {
        var errors = new List<Error>();
        var cover = (d.CoverLetter ?? string.Empty).Trim();
        var certifications = string.IsNullOrWhiteSpace(d.Certifications) ? null : d.Certifications.Trim();

        if (cover.Length is < 30 or > 3000) errors.Add(CareersErrors.Field("coverLetter", "The cover letter needs 30 to 3000 characters."));
        if (d.YearsExperience is < 0 or > 50) errors.Add(CareersErrors.Field("yearsExperience", "Years of experience must be from 0 to 50."));
        if (certifications is { Length: > 500 }) errors.Add(CareersErrors.Field("certifications", "Certifications can be at most 500 characters."));
        if (d.AvailableFrom is DateOnly from && from < today) errors.Add(CareersErrors.Field("availableFrom", "The start date cannot be in the past."));
        if (d.ExpectedSalaryAmount is <= 0) errors.Add(CareersErrors.Field("expectedSalary", "Expected salary must be more than 0."));
        if (errors.Count > 0)
        {
            return errors;
        }

        var application = new JobApplication
        {
            JobId = d.JobId,
            CustomerId = customerId,
            CandidateName = candidateName,
            CandidateEmail = candidateEmail,
            CandidatePhone = candidatePhone,
            CvFileId = d.CvId,
            CoverLetter = cover,
            YearsExperience = d.YearsExperience,
            Certifications = certifications,
            AvailableFrom = d.AvailableFrom,
            ExpectedSalaryAmount = d.ExpectedSalaryAmount,
            SubmittedAtUtc = DateTimeOffset.UtcNow
        };
        application.AppendHistory(ApplicationStatuses.Submitted, "candidate");
        return application;
    }

    private void AppendHistory(string status, string by)
        => HistoryJson = JsonSerializer.Serialize(History.Append(new HistoryEntry(status, DateTimeOffset.UtcNow, by)).ToList());

    /// <summary>The employer moves the application one step, or rejects it; a note (up to 500 characters) goes to the candidate.</summary>
    public Result<Updated> EmployerMove(string status, string? note)
    {
        if (!ApplicationStatuses.EmployerMoves(Status).Contains(status))
        {
            return CareersErrors.IllegalApplicationMove(Status, status);
        }

        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        StatusNote = trimmed is { Length: > 500 } ? trimmed[..500] : trimmed;
        Status = status;
        AppendHistory(status, "employer");
        return Result.Updated;
    }

    /// <summary>The candidate withdraws. The reason is stored on its own and never overwrites the status note.</summary>
    public Result<Updated> Withdraw(string? reason)
    {
        if (!ApplicationStatuses.CanWithdraw(Status))
        {
            return CareersErrors.IllegalApplicationMove(Status, ApplicationStatuses.Withdrawn);
        }

        var trimmed = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        WithdrawReason = trimmed is { Length: > 500 } ? trimmed[..500] : trimmed;
        Status = ApplicationStatuses.Withdrawn;
        AppendHistory(ApplicationStatuses.Withdrawn, "candidate");
        return Result.Updated;
    }

    public Result<Updated> SetEmployerNote(string? note)
    {
        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (trimmed is { Length: > 2000 })
        {
            return CareersErrors.Field("employerNote", "The private note can be at most 2000 characters.");
        }

        EmployerNote = trimmed;
        return Result.Updated;
    }

    /// <summary>Opening an application marks it seen, so it is no longer "new".</summary>
    public void MarkSeen() => SeenByEmployerAtUtc ??= DateTimeOffset.UtcNow;
}

public static class CareersErrors
{
    public static readonly Error EmployerNotFound = Error.NotFound("Careers.EmployerNotFound", "That employer does not exist.");
    public static readonly Error JobNotFound = Error.NotFound("Careers.JobNotFound", "That job does not exist.");
    public static readonly Error ApplicationNotFound = Error.NotFound("Careers.ApplicationNotFound", "That application does not exist.");
    public static readonly Error CvNotFound = Error.NotFound("Careers.CvNotFound", "That file does not exist.");
    public static readonly Error CategoryNotFound = Error.NotFound("Careers.CategoryNotFound", "That category does not exist.");

    public static Error Field(string field, string message)
        => Error.Validation($"Careers.{field}", message).ForField(field);

    public static Error Conflict(string message) => Error.Conflict("Careers.Conflict", message);

    public static readonly Error EmployerNotVerified = Error.Conflict("Careers.EmployerNotVerified", "Verify the employer before approving its jobs.");
    public static readonly Error EmployerBlocked = Error.Forbidden("Careers.EmployerBlocked", "Your company account cannot post jobs right now. Contact JoFit.");
    public static readonly Error NotAcceptingApplications = Error.Conflict("Careers.NotAccepting", "This job is no longer taking applications.");
    public static readonly Error AlreadyApplied = Error.Conflict("Careers.AlreadyApplied", "You have already applied to this job.");
    public static readonly Error HasApplications = Error.Conflict("Careers.HasApplications", "People have applied to this job. Close it instead of deleting it.");

    public static Error IllegalEmployerMove(string from, string to)
        => Error.Conflict("Careers.IllegalEmployerMove", $"A {from} employer cannot become {to}.");

    public static Error IllegalJobMove(string from, string to)
        => Error.Conflict("Careers.IllegalJobMove", $"A {from} job cannot become {to}.");

    public static Error IllegalApplicationMove(string from, string to)
        => Error.Conflict("Careers.IllegalApplicationMove", $"A {from} application cannot become {to}.");

    public static Error CategoryInUse(int jobs) => Error.Conflict("Careers.CategoryInUse", $"This category is used by {jobs} job(s). Move them first.");
}
