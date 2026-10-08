using JoFit.Domain.Common.Results;

namespace JoFit.Domain.UniversityMajors;

public static class UniversityMajorErrors
{
    public static readonly Error NameArRequired = Error.Validation(
        "UniversityMajor.NameArRequired",
        "Arabic name is required");

    public static readonly Error NameArMaxLengthExceeded = Error.Validation(
        "UniversityMajor.NameArMaxLengthExceeded",
        "Arabic name cannot exceed 100 characters");

    public static readonly Error NameEnRequired = Error.Validation(
        "UniversityMajor.NameEnRequired",
        "English name is required");

    public static readonly Error NameEnMaxLengthExceeded = Error.Validation(
        "UniversityMajor.NameEnMaxLengthExceeded",
        "English name cannot exceed 100 characters");

    public static readonly Error UniversityMajorNotFound = Error.NotFound(
        "UniversityMajor.NotFound",
        "University major not found");
}
