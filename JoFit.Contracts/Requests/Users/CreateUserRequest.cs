using System.ComponentModel.DataAnnotations;

namespace JoFit.Contracts.Requests.Users;

/// <summary>
/// Shared by the normal user, coach and portal user creation endpoints; the role is decided by the endpoint.
/// </summary>
public record CreateUserRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(256, MinimumLength = 3)]
    public required string UserName { get; init; }

    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    [StringLength(256)]
    public required string Email { get; init; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(256, MinimumLength = 6)]
    public required string Password { get; init; }

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
}
