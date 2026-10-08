using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Countries;

public static class CountryErrors
{
    public static readonly Error NameArRequired = Error.Validation(
        "Country.NameArRequired",
        "Arabic name is required");

    public static readonly Error NameArMaxLengthExceeded = Error.Validation(
        "Country.NameArMaxLengthExceeded",
        "Arabic name cannot exceed 100 characters");

    public static readonly Error NameEnRequired = Error.Validation(
        "Country.NameEnRequired",
        "English name is required");

    public static readonly Error NameEnMaxLengthExceeded = Error.Validation(
        "Country.NameEnMaxLengthExceeded",
        "English name cannot exceed 100 characters");

    public static readonly Error CountryNotFound = Error.NotFound(
        "Country.NotFound",
        "Country not found");
}
