using System.ComponentModel.DataAnnotations;

namespace JoFit.Contracts.Requests.UserExperiences;

public sealed record UserExperienceRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(300)]
    public required string Address { get; init; }

    [Range(1, long.MaxValue)]
    public long JobTypeId { get; init; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(100)]
    public required string CompanyName { get; init; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(100)]
    public required string CompanyAddress { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime? EndDate { get; init; }

    public bool IsStillWorking { get; init; }
}
