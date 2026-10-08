using System.ComponentModel.DataAnnotations;

namespace JoFit.Contracts.Requests.Roles;

public sealed record CreateRoleRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(256, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>
    /// Optional. If provided, the new role inherits all permissions from this role.
    /// </summary>
    public long? CloneFromRoleId { get; init; }
}
