using System.ComponentModel.DataAnnotations;

namespace JoFit.Contracts.Requests.UniversityMajors;

public sealed record CreateUniversityMajorRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 1)]
    public required string NameAr { get; init; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 1)]
    public required string NameEn { get; init; }

    public bool IsActive { get; init; } = true;
}
