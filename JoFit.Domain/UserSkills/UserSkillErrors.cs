using JoFit.Domain.Common.Results;

namespace JoFit.Domain.UserSkills;

public static class UserSkillErrors
{
    public static readonly Error UserIdRequired = Error.Validation(
        "UserSkill.UserIdRequired",
        "User is required");

    public static readonly Error CourseIdRequired = Error.Validation(
        "UserSkill.CourseIdRequired",
        "Course is required");

    public static readonly Error PercentageScoreOutOfRange = Error.Validation(
        "UserSkill.PercentageScoreOutOfRange",
        "Percentage score must be between 0 and 100");

    public static readonly Error UserSkillNotFound = Error.NotFound(
        "UserSkill.NotFound",
        "User skill not found");
}
