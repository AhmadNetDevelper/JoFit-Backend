using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.JobStatuses;

public class JobStatus : AuditableEntity
{
    public string NameAr { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private JobStatus()
    {
    }

    private JobStatus(string nameAr, string nameEn, bool isActive) : base()
    {
        NameAr = nameAr;
        NameEn = nameEn;
        IsActive = isActive;
    }

    /// <summary>
    /// Creates a new JobStatus instance with validation.
    /// </summary>
    public static Result<JobStatus> Create(string nameAr, string nameEn, bool isActive = true)
    {
        var validation = Validate(nameAr, nameEn);
        if (validation is not null)
        {
            return validation.Value;
        }

        return new JobStatus(nameAr, nameEn, isActive);
    }

    /// <summary>
    /// Updates the job status information with validation.
    /// </summary>
    public Result<Updated> Update(string nameAr, string nameEn, bool isActive)
    {
        var validation = Validate(nameAr, nameEn);
        if (validation is not null)
        {
            return validation.Value;
        }

        NameAr = nameAr;
        NameEn = nameEn;
        IsActive = isActive;

        return Result.Updated;
    }

    private static Error? Validate(string nameAr, string nameEn)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
        {
            return JobStatusErrors.NameArRequired;
        }

        if (nameAr.Length > 100)
        {
            return JobStatusErrors.NameArMaxLengthExceeded;
        }

        if (string.IsNullOrWhiteSpace(nameEn))
        {
            return JobStatusErrors.NameEnRequired;
        }

        if (nameEn.Length > 100)
        {
            return JobStatusErrors.NameEnMaxLengthExceeded;
        }

        return null;
    }
}
