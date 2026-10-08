using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.AcademicDegrees;

public class AcademicDegree : AuditableEntity
{
    public string NameAr { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private AcademicDegree()
    {
    }

    private AcademicDegree(string nameAr, string nameEn, bool isActive) : base()
    {
        NameAr = nameAr;
        NameEn = nameEn;
        IsActive = isActive;
    }

    /// <summary>
    /// Creates a new AcademicDegree instance with validation.
    /// </summary>
    public static Result<AcademicDegree> Create(string nameAr, string nameEn, bool isActive = true)
    {
        var validation = Validate(nameAr, nameEn);
        if (validation is not null)
        {
            return validation.Value;
        }

        return new AcademicDegree(nameAr, nameEn, isActive);
    }

    /// <summary>
    /// Updates the academic degree information with validation.
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
            return AcademicDegreeErrors.NameArRequired;
        }

        if (nameAr.Length > 100)
        {
            return AcademicDegreeErrors.NameArMaxLengthExceeded;
        }

        if (string.IsNullOrWhiteSpace(nameEn))
        {
            return AcademicDegreeErrors.NameEnRequired;
        }

        if (nameEn.Length > 100)
        {
            return AcademicDegreeErrors.NameEnMaxLengthExceeded;
        }

        return null;
    }
}
