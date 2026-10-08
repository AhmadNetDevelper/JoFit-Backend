using JoFit.Domain.Common.Results;

namespace JoFit.Domain.JobStatuses;

public static class JobStatusErrors
{
    public static readonly Error NameArRequired = Error.Validation(
        "JobStatus.NameArRequired",
        "Arabic name is required");

    public static readonly Error NameArMaxLengthExceeded = Error.Validation(
        "JobStatus.NameArMaxLengthExceeded",
        "Arabic name cannot exceed 100 characters");

    public static readonly Error NameEnRequired = Error.Validation(
        "JobStatus.NameEnRequired",
        "English name is required");

    public static readonly Error NameEnMaxLengthExceeded = Error.Validation(
        "JobStatus.NameEnMaxLengthExceeded",
        "English name cannot exceed 100 characters");

    public static readonly Error JobStatusNotFound = Error.NotFound(
        "JobStatus.NotFound",
        "Job status not found");
}
