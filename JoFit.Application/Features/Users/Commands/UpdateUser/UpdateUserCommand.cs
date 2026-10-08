using JoFit.Application.Features.Users.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity.Users;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace JoFit.Application.Features.Users.Commands.UpdateUser;

public sealed record UpdateUserCommand(
    long UserId,
    string UserName,
    string Email,
    string PhoneNumber,
    string FullName,
    DateTime? BirthOfDate,
    EnumGender GenderId,
    long? CountryId,
    long? CityId,
    long? JobStatusId,
    bool IsActive,
    IFormFile? ProfileImage = null) : IRequest<Result<UserDto>>;
