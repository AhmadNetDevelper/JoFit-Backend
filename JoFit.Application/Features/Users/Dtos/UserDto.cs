using JoFit.Domain.Identity.Users;

namespace JoFit.Application.Features.Users.Dtos;

public sealed record UserDto(
    long UserId,
    string UserName,
    string Email,
    string? PhoneNumber,
    string FullName,
    DateTime? BirthOfDate,
    bool IsActive,
    string? ProfileImageUrl,
    EnumGender GenderId,
    long? CountryId,
    long? CityId,
    long? JobStatusId,
    long? RoleId,
    string? RoleName);
