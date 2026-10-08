using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Courses;

public static class CourseErrors
{
    public static readonly Error NameArRequired = Error.Validation(
        "Course.NameArRequired",
        "Arabic name is required");

    public static readonly Error NameArMaxLengthExceeded = Error.Validation(
        "Course.NameArMaxLengthExceeded",
        "Arabic name cannot exceed 100 characters");

    public static readonly Error NameEnRequired = Error.Validation(
        "Course.NameEnRequired",
        "English name is required");

    public static readonly Error NameEnMaxLengthExceeded = Error.Validation(
        "Course.NameEnMaxLengthExceeded",
        "English name cannot exceed 100 characters");

    public static readonly Error CourseNotFound = Error.NotFound(
        "Course.NotFound",
        "Course not found");

    /// <summary>A validation error on one field of the catalogue form (dotted for nested, e.g. nextIntake.startsOn).</summary>
    public static Error Field(string field, string message)
        => Error.Validation($"Course.{field}", message).ForField(field);

    public static readonly Error SlugTaken = Field("slug", "Another course already uses this slug.");

    public static Error HasEnrolments(int count) => Error.Conflict(
        "Course.HasEnrolments",
        $"{count} enrolment(s) are still pending or active. Archive the course instead.");
}
