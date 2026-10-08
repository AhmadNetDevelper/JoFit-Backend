using System.ComponentModel.DataAnnotations;

namespace JoFit.Contracts.Requests.Users;

public record UpdateUserRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(256, MinimumLength = 3)]
    public required string UserName { get; init; }

    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    [StringLength(256)]
    public required string Email { get; init; }

    [Required(AllowEmptyStrings = false)]
    [Phone]
    public required string PhoneNumber { get; init; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(200)]
    public required string FullName { get; init; }

    public DateTime? BirthOfDate { get; init; }

    /// <summary>1 = Male, 2 = Female.</summary>
    [Range(1, 2)]
    public short GenderId { get; init; }

    public long? CountryId { get; init; }

    public long? CityId { get; init; }

    public long? JobStatusId { get; init; }

    public bool IsActive { get; init; } = true;
}
