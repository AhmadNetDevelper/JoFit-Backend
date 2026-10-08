using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.UserJobTypes;

public class UserJobType : AuditableEntity
{
    public string NameAr { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool IsNormalUser { get; private set; }

    private UserJobType()
    {
    }

    private UserJobType(string nameAr, string nameEn, bool isActive, bool isNormalUser) : base()
    {
        NameAr = nameAr;
        NameEn = nameEn;
        IsActive = isActive;
        IsNormalUser = isNormalUser;
    }

    /// <summary>
    /// Creates a new UserJobType instance with validation.
    /// </summary>
    public static Result<UserJobType> Create(string nameAr, string nameEn, bool isActive = true, bool isNormalUser = false)
    {
        var validation = Validate(nameAr, nameEn);
        if (validation is not null)
        {
            return validation.Value;
        }

        return new UserJobType(nameAr, nameEn, isActive, isNormalUser);
    }

    /// <summary>
    /// Updates the user type information with validation.
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
            return UserJobTypeErrors.NameArRequired;
        }

        if (nameAr.Length > 100)
        {
            return UserJobTypeErrors.NameArMaxLengthExceeded;
        }

        if (string.IsNullOrWhiteSpace(nameEn))
        {
            return UserJobTypeErrors.NameEnRequired;
        }

        if (nameEn.Length > 100)
        {
            return UserJobTypeErrors.NameEnMaxLengthExceeded;
        }

        return null;
    }
}
