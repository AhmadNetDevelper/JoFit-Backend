using JoFit.Domain.Common.Results;

namespace JoFit.Domain.JobTypes;

public static class JobTypeErrors
{
    public static readonly Error NameArRequired = Error.Validation(
        "JobType.NameArRequired",
        "Arabic name is required");

    public static readonly Error NameArMaxLengthExceeded = Error.Validation(
        "JobType.NameArMaxLengthExceeded",
        "Arabic name cannot exceed 100 characters");

    public static readonly Error NameEnRequired = Error.Validation(
        "JobType.NameEnRequired",
        "English name is required");

    public static readonly Error NameEnMaxLengthExceeded = Error.Validation(
        "JobType.NameEnMaxLengthExceeded",
        "English name cannot exceed 100 characters");

    public static readonly Error JobTypeNotFound = Error.NotFound(
        "JobType.NotFound",
        "Job type not found");
}
