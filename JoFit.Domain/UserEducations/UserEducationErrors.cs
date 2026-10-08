using JoFit.Domain.Common.Results;

namespace JoFit.Domain.UserEducations;

public static class UserEducationErrors
{
    public static readonly Error UserIdRequired = Error.Validation(
        "UserEducation.UserIdRequired",
        "User is required");

    public static readonly Error SchoolNameRequired = Error.Validation(
        "UserEducation.SchoolNameRequired",
        "School name is required");

    public static readonly Error SchoolNameMaxLengthExceeded = Error.Validation(
        "UserEducation.SchoolNameMaxLengthExceeded",
        "School name cannot exceed 100 characters");

    public static readonly Error AcademicDegreeIdRequired = Error.Validation(
        "UserEducation.AcademicDegreeIdRequired",
        "Academic degree is required");

    public static readonly Error UniversityMajorIdRequired = Error.Validation(
        "UserEducation.UniversityMajorIdRequired",
        "University major is required");

    public static readonly Error EndDateBeforeStartDate = Error.Validation(
        "UserEducation.EndDateBeforeStartDate",
        "End date cannot be before start date");

    public static readonly Error UserEducationNotFound = Error.NotFound(
        "UserEducation.NotFound",
        "User education not found");
}
