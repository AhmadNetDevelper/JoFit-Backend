using System.ComponentModel.DataAnnotations;

namespace JoFit.Contracts.Requests.Roles;

public sealed record UpdateRoleRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(256, MinimumLength = 1)]
    public required string Name { get; init; }
}
