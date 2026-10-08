using System.ComponentModel.DataAnnotations;

namespace JoFit.Contracts.Requests.UserSkills;

public sealed record UserSkillRequest
{
    [Range(1, long.MaxValue)]
    public long CourseId { get; init; }

    [Range(0, 100)]
    public short PercentageScore { get; init; }
}
