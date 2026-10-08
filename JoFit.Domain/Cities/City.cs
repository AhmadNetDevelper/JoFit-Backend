using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Countries;

namespace JoFit.Domain.Cities;

public class City : AuditableEntity
{
    public string NameAr { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public long CountryId { get; private set; }
    public bool IsActive { get; private set; }

    public Country? Country { get; private set; }

    private City()
    {
    }

    private City(string nameAr, string nameEn, long countryId, bool isActive) : base()
    {
        NameAr = nameAr;
        NameEn = nameEn;
        CountryId = countryId;
        IsActive = isActive;
    }

    /// <summary>
    /// Creates a new City instance with validation.
    /// </summary>
    public static Result<City> Create(string nameAr, string nameEn, long countryId, bool isActive = true)
    {
        var validation = Validate(nameAr, nameEn, countryId);
        if (validation is not null)
        {
            return validation.Value;
        }

        return new City(nameAr, nameEn, countryId, isActive);
    }

    /// <summary>
    /// Updates the city information with validation.
    /// </summary>
    public Result<Updated> Update(string nameAr, string nameEn, long countryId, bool isActive)
    {
        var validation = Validate(nameAr, nameEn, countryId);
        if (validation is not null)
        {
            return validation.Value;
        }

        NameAr = nameAr;
        NameEn = nameEn;
        CountryId = countryId;
        IsActive = isActive;

        return Result.Updated;
    }

    private static Error? Validate(string nameAr, string nameEn, long countryId)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
        {
            return CityErrors.NameArRequired;
        }

        if (nameAr.Length > 100)
        {
            return CityErrors.NameArMaxLengthExceeded;
        }

        if (string.IsNullOrWhiteSpace(nameEn))
        {
            return CityErrors.NameEnRequired;
        }

        if (nameEn.Length > 100)
        {
            return CityErrors.NameEnMaxLengthExceeded;
        }

        if (countryId <= 0)
        {
            return CityErrors.CountryIdRequired;
        }

        return null;
    }
}
