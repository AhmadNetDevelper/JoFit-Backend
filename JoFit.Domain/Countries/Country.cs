using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Countries;

public class Country : AuditableEntity
{
    public string NameAr { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private Country()
    {
    }

    private Country(string nameAr, string nameEn, bool isActive) : base()
    {
        NameAr = nameAr;
        NameEn = nameEn;
        IsActive = isActive;
    }

    /// <summary>
    /// Creates a new Country instance with validation.
    /// </summary>
    public static Result<Country> Create(string nameAr, string nameEn, bool isActive = true)
    {
        var validation = Validate(nameAr, nameEn);
        if (validation is not null)
        {
            return validation.Value;
        }

        return new Country(nameAr, nameEn, isActive);
    }

    /// <summary>
    /// Updates the country information with validation.
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
            return CountryErrors.NameArRequired;
        }

        if (nameAr.Length > 100)
        {
            return CountryErrors.NameArMaxLengthExceeded;
        }

        if (string.IsNullOrWhiteSpace(nameEn))
        {
            return CountryErrors.NameEnRequired;
        }

        if (nameEn.Length > 100)
        {
            return CountryErrors.NameEnMaxLengthExceeded;
        }

        return null;
    }
}
