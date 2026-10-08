using System.ComponentModel.DataAnnotations;

namespace JoFit.Contracts.Requests.JobStatuses;

public sealed record UpdateJobStatusRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 1)]
    public required string NameAr { get; init; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 1)]
    public required string NameEn { get; init; }

    [Required]
    public required bool IsActive { get; init; }
}
