using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.JobTypes;

public class JobType : AuditableEntity
{
    public string NameAr { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private JobType()
    {
    }

    private JobType(string nameAr, string nameEn, bool isActive) : base()
    {
        NameAr = nameAr;
        NameEn = nameEn;
        IsActive = isActive;
    }

    /// <summary>
    /// Creates a new JobType instance with validation.
    /// </summary>
    public static Result<JobType> Create(string nameAr, string nameEn, bool isActive = true)
    {
        var validation = Validate(nameAr, nameEn);
        if (validation is not null)
        {
            return validation.Value;
        }

        return new JobType(nameAr, nameEn, isActive);
    }

    /// <summary>
    /// Updates the job type information with validation.
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
            return JobTypeErrors.NameArRequired;
        }

        if (nameAr.Length > 100)
        {
            return JobTypeErrors.NameArMaxLengthExceeded;
        }

        if (string.IsNullOrWhiteSpace(nameEn))
        {
            return JobTypeErrors.NameEnRequired;
        }

        if (nameEn.Length > 100)
        {
            return JobTypeErrors.NameEnMaxLengthExceeded;
        }

        return null;
    }
}
