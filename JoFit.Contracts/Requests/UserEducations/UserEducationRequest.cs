using System.ComponentModel.DataAnnotations;

namespace JoFit.Contracts.Requests.UserEducations;

public sealed record UserEducationRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(100)]
    public required string SchoolName { get; init; }

    [Range(1, long.MaxValue)]
    public long AcademicDegreeId { get; init; }

    [Range(1, long.MaxValue)]
    public long UniversityMajorId { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime? EndDate { get; init; }

    public bool IsStillEducation { get; init; }
}
