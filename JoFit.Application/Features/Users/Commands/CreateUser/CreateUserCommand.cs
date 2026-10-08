using JoFit.Application.Features.Users.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity.Users;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace JoFit.Application.Features.Users.Commands.CreateUser;

/// <param name="RoleName">Role the user is created with (User or Coach).</param>
public sealed record CreateUserCommand(
    string UserName,
    string Email,
    string Password,
    string PhoneNumber,
    string FullName,
    DateTime? BirthOfDate,
    EnumGender GenderId,
    long? CountryId,
    long? CityId,
    long? JobStatusId,
    string RoleName,
    IFormFile? ProfileImage = null) : IRequest<Result<UserDto>>;
