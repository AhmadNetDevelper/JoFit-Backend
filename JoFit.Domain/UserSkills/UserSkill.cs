using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Courses;

namespace JoFit.Domain.UserSkills;

public class UserSkill : AuditableEntity
{
    public long UserId { get; private set; }
    public long CourseId { get; private set; }
    public short PercentageScore { get; private set; }

    public Course? Course { get; private set; }

    private UserSkill()
    {
    }

    private UserSkill(long userId, long courseId, short percentageScore) : base()
    {
        UserId = userId;
        CourseId = courseId;
        PercentageScore = percentageScore;
    }

    /// <summary>
    /// Creates a new UserSkill instance with validation.
    /// </summary>
    public static Result<UserSkill> Create(long userId, long courseId, short percentageScore)
    {
        if (userId <= 0)
        {
            return UserSkillErrors.UserIdRequired;
        }

        var validation = Validate(courseId, percentageScore);
        if (validation is not null)
        {
            return validation.Value;
        }

        return new UserSkill(userId, courseId, percentageScore);
    }

    /// <summary>
    /// Updates the skill information with validation.
    /// </summary>
    public Result<Updated> Update(long courseId, short percentageScore)
    {
        var validation = Validate(courseId, percentageScore);
        if (validation is not null)
        {
            return validation.Value;
        }

        CourseId = courseId;
        PercentageScore = percentageScore;

        return Result.Updated;
    }

    private static Error? Validate(long courseId, short percentageScore)
    {
        if (courseId <= 0)
        {
            return UserSkillErrors.CourseIdRequired;
        }

        if (percentageScore is < 0 or > 100)
        {
            return UserSkillErrors.PercentageScoreOutOfRange;
        }

        return null;
    }
}
