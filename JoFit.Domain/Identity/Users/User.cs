using Microsoft.AspNetCore.Identity;
using JoFit.Domain.Identity;

namespace JoFit.Domain.Identity.Users;

public class User : IdentityUser<long>
{
    public string FullName { get; set; } = string.Empty;
    public DateTime? BirthOfDate { get; set; }
    public bool IsActive { get; set; }
    public string? ProfileImageUrl { get; set; }
    public EnumGender GenderId { get; set; }
    public long? CountryId { get; set; }
    public long? CityId { get; set; }
    public long? JobStatusId { get; set; }

    public ICollection<UserPermission> UserPermissions { get; } = [];
}