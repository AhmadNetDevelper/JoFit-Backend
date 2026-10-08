using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Courses;

public partial class Course : AuditableEntity
{
    public string NameAr { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private Course()
    {
    }

    private Course(string nameAr, string nameEn, bool isActive) : base()
    {
        NameAr = nameAr;
        NameEn = nameEn;
        IsActive = isActive;
    }

    /// <summary>
    /// Creates a new Course instance with validation.
    /// </summary>
    public static Result<Course> Create(string nameAr, string nameEn, bool isActive = true)
    {
        var validation = Validate(nameAr, nameEn);
        if (validation is not null)
        {
            return validation.Value;
        }

        return new Course(nameAr, nameEn, isActive);
    }

    /// <summary>
    /// Updates the course information with validation.
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
            return CourseErrors.NameArRequired;
        }

        if (nameAr.Length > 100)
        {
            return CourseErrors.NameArMaxLengthExceeded;
        }

        if (string.IsNullOrWhiteSpace(nameEn))
        {
            return CourseErrors.NameEnRequired;
        }

        if (nameEn.Length > 100)
        {
            return CourseErrors.NameEnMaxLengthExceeded;
        }

        return null;
    }
}
