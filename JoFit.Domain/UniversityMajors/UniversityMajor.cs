using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.UniversityMajors;

public class UniversityMajor : AuditableEntity
{
    public string NameAr { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private UniversityMajor()
    {
    }

    private UniversityMajor(string nameAr, string nameEn, bool isActive) : base()
    {
        NameAr = nameAr;
        NameEn = nameEn;
        IsActive = isActive;
    }

    /// <summary>
    /// Creates a new UniversityMajor instance with validation.
    /// </summary>
    public static Result<UniversityMajor> Create(string nameAr, string nameEn, bool isActive = true)
    {
        var validation = Validate(nameAr, nameEn);
        if (validation is not null)
        {
            return validation.Value;
        }

        return new UniversityMajor(nameAr, nameEn, isActive);
    }

    /// <summary>
    /// Updates the university major information with validation.
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
            return UniversityMajorErrors.NameArRequired;
        }

        if (nameAr.Length > 100)
        {
            return UniversityMajorErrors.NameArMaxLengthExceeded;
        }

        if (string.IsNullOrWhiteSpace(nameEn))
        {
            return UniversityMajorErrors.NameEnRequired;
        }

        if (nameEn.Length > 100)
        {
            return UniversityMajorErrors.NameEnMaxLengthExceeded;
        }

        return null;
    }
}
