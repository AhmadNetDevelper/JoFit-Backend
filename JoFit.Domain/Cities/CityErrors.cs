using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Cities;

public static class CityErrors
{
    public static readonly Error NameArRequired = Error.Validation(
        "City.NameArRequired",
        "Arabic name is required");

    public static readonly Error NameArMaxLengthExceeded = Error.Validation(
        "City.NameArMaxLengthExceeded",
        "Arabic name cannot exceed 100 characters");

    public static readonly Error NameEnRequired = Error.Validation(
        "City.NameEnRequired",
        "English name is required");

    public static readonly Error NameEnMaxLengthExceeded = Error.Validation(
        "City.NameEnMaxLengthExceeded",
        "English name cannot exceed 100 characters");

    public static readonly Error CountryIdRequired = Error.Validation(
        "City.CountryIdRequired",
        "Country is required");

    public static readonly Error CityNotFound = Error.NotFound(
        "City.NotFound",
        "City not found");
}
