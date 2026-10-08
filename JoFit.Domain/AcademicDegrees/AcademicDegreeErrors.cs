using JoFit.Domain.Common.Results;

namespace JoFit.Domain.AcademicDegrees;

public static class AcademicDegreeErrors
{
    public static readonly Error NameArRequired = Error.Validation(
        "AcademicDegree.NameArRequired",
        "Arabic name is required");

    public static readonly Error NameArMaxLengthExceeded = Error.Validation(
        "AcademicDegree.NameArMaxLengthExceeded",
        "Arabic name cannot exceed 100 characters");

    public static readonly Error NameEnRequired = Error.Validation(
        "AcademicDegree.NameEnRequired",
        "English name is required");

    public static readonly Error NameEnMaxLengthExceeded = Error.Validation(
        "AcademicDegree.NameEnMaxLengthExceeded",
        "English name cannot exceed 100 characters");

    public static readonly Error AcademicDegreeNotFound = Error.NotFound(
        "AcademicDegree.NotFound",
        "Academic degree not found");
}
