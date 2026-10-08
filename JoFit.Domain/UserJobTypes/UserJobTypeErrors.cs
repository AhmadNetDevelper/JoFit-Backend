using JoFit.Domain.Common.Results;

namespace JoFit.Domain.UserJobTypes;

public static class UserJobTypeErrors
{
    public static readonly Error NameArRequired = Error.Validation(
        "UserJobType.NameArRequired",
        "Arabic name is required");

    public static readonly Error NameArMaxLengthExceeded = Error.Validation(
        "UserJobType.NameArMaxLengthExceeded",
        "Arabic name cannot exceed 100 characters");

    public static readonly Error NameEnRequired = Error.Validation(
        "UserJobType.NameEnRequired",
        "English name is required");

    public static readonly Error NameEnMaxLengthExceeded = Error.Validation(
        "UserJobType.NameEnMaxLengthExceeded",
        "English name cannot exceed 100 characters");

    public static readonly Error UserJobTypeNotFound = Error.NotFound(
        "UserJobType.NotFound",
        "User type not found");
}
