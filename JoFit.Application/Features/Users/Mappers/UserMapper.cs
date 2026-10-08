using JoFit.Application.Features.Users.Dtos;
using JoFit.Domain.Identity.Users;

namespace JoFit.Application.Features.Users.Mappers;

public static class UserMapper
{
    public static UserDto ToDto(this User user, long? roleId, string? roleName)
        => new(
            user.Id,
            user.UserName!,
            user.Email!,
            user.PhoneNumber,
            user.FullName,
            user.BirthOfDate,
            user.IsActive,
            user.ProfileImageUrl,
            user.GenderId,
            user.CountryId,
            user.CityId,
            user.JobStatusId,
            roleId,
            roleName);
}
