using System.ComponentModel.DataAnnotations;

namespace JoFit.Contracts.Requests.Cities;

public sealed record UpdateCityRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 1)]
    public required string NameAr { get; init; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 1)]
    public required string NameEn { get; init; }

    [Required]
    [Range(1, long.MaxValue)]
    public required long CountryId { get; init; }

    [Required]
    public required bool IsActive { get; init; }
}
