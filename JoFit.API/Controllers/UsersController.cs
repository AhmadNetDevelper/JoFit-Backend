using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.API.Mapping;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Users.Commands.CreateCoach;
using JoFit.Application.Features.Users.Commands.CreateUser;
using JoFit.Application.Features.Users.Commands.RemoveUser;
using JoFit.Application.Features.Users.Commands.UpdatePermissionsForUser;
using JoFit.Application.Features.Users.Commands.UpdateUser;
using JoFit.Application.Features.Users.Dtos;
using JoFit.Application.Features.Users.Queries.GetDirectUserPermissions;
using JoFit.Application.Features.Users.Queries.GetRoleUserPermissions;
using JoFit.Application.Features.Users.Queries.GetUserById;
using JoFit.Application.Features.Users.Queries.GetUsersPaged;
using JoFit.Contracts.Requests.Users;
using JoFit.Domain.Identity.Users;
using JoFit.Infrastructure.Data.Seeders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/users")]
[ApiVersion("1.0")]
[JoFit.API.OpenApi.SharedApi]
public sealed class UsersController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Users.Read)]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Retrieves a paged list of users.")]
    [EndpointDescription("Supports optional search (name, username, email, phone), filtering by active status and by role name.")]
    [EndpointName("GetUsersPaged")]
    public async Task<IActionResult> GetPaged([FromQuery] GetUsersPagedRequest request, CancellationToken ct)
    {
        var query = new GetUsersPagedQuery(request.Page, request.PageSize, request.Search, request.IsActive, request.RoleName);
        var result = await sender.Send(query, ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("{userId:long}", Name = "GetUserById")]
    [Authorize(Permissions.Users.Read)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieves a user by ID.")]
    [EndpointName("GetUserById")]
    public async Task<IActionResult> GetById(long userId, CancellationToken ct)
    {
        var result = await sender.Send(new GetUserByIdQuery(userId), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost("normal-user")]
    [Authorize(Permissions.Users.Create)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Creates a normal user.")]
    [EndpointDescription("Creates a user with the User role. Multipart form; the optional ProfileImage is uploaded and its link stored on the user.")]
    [Consumes("multipart/form-data")]
    [EndpointName("CreateNormalUser")]
    public Task<IActionResult> CreateNormalUser([FromForm] CreateUserWithImageRequest request, CancellationToken ct)
        => CreateAsync(request, SeedData.Roles.User, request.ProfileImage, ct);

    [HttpPost("coach")]
    [Authorize(Permissions.Users.Create)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Creates a coach.")]
    [EndpointDescription("Creates a user with the Coach role together with their educations, certificates, experiences and skills. Multipart form with these parts: User (login and personal data), Profile (the coach page: slug, name, headline, location, sportIds, languages, packages), Educations[i], Certificates[i] (optional CertificateFile), Experiences[i] and Skills[i]. Everything is saved in one transaction.")]
    [Consumes("multipart/form-data")]
    [EndpointName("CreateCoach")]
    public async Task<IActionResult> CreateCoach([FromForm] CreateCoachRequest request, CancellationToken ct)
    {
        var command = request.ToCommand();

        var result = await sender.Send(command, ct);
        return result.Match(
            dto => CreatedAtRoute("GetUserById", new { version = "1.0", userId = dto.UserId }, dto),
            Problem);
    }

    [HttpPut("{userId:long}")]
    [Authorize(Permissions.Users.Update)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Updates an existing user.")]
    [EndpointDescription("Updates the user's profile data. The role is fixed by the endpoint the user was created with.")]
    [EndpointName("UpdateUser")]
    public async Task<IActionResult> UpdateUser(long userId, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var command = new UpdateUserCommand(
            userId,
            request.UserName,
            request.Email,
            request.PhoneNumber,
            request.FullName,
            request.BirthOfDate,
            (EnumGender)request.GenderId,
            request.CountryId,
            request.CityId,
            request.JobStatusId,
            request.IsActive);

        var result = await sender.Send(command, ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpDelete("{userId:long}")]
    [Authorize(Permissions.Users.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Removes a user.")]
    [EndpointName("RemoveUser")]
    public async Task<IActionResult> Delete(long userId, CancellationToken ct)
    {
        var result = await sender.Send(new RemoveUserCommand(userId), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpGet("{userId:long}/role-based-permissions")]
    [Authorize(Permissions.Users.Read)]
    [ProducesResponseType(typeof(List<UserPermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieves the role based permissions of a user.")]
    [EndpointDescription("Returns the permissions the user gets through their role. IsGranted is false where a direct override revokes one.")]
    [EndpointName("GetRoleUserPermissions")]
    public async Task<IActionResult> GetRoleUserPermissions(long userId, CancellationToken ct)
    {
        var result = await sender.Send(new GetRoleUserPermissionsQuery(userId), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("{userId:long}/direct-permissions")]
    [Authorize(Permissions.Users.Read)]
    [ProducesResponseType(typeof(List<UserPermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieves the direct permissions of a user.")]
    [EndpointDescription("Returns the permissions that are not part of the user's role. IsGranted is true where granted directly to the user.")]
    [EndpointName("GetDirectUserPermissions")]
    public async Task<IActionResult> GetDirectUserPermissions(long userId, CancellationToken ct)
    {
        var result = await sender.Send(new GetDirectUserPermissionsQuery(userId), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPut("{userId:long}/permissions")]
    [Authorize(Permissions.Users.Update)]
    [ProducesResponseType(typeof(List<UserPermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Updates permissions for a user.")]
    [EndpointDescription("Grants or revokes permissions for the user on top of their role. Returns the user's resulting direct overrides.")]
    [EndpointName("UpdatePermissionsForUser")]
    public async Task<IActionResult> UpdatePermissionsForUser(long userId, [FromBody] UpdatePermissionsForUserRequest request, CancellationToken ct)
    {
        var permissions = request.Permissions
            .Select(p => new UserPermissionCommandRequest(p.PermissionId, p.IsGranted))
            .ToList();

        var result = await sender.Send(new UpdatePermissionsForUserCommand(userId, permissions), ct);
        return result.Match(response => Ok(response), Problem);
    }

    private async Task<IActionResult> CreateAsync(CreateUserRequest request, string roleName, IFormFile? profileImage, CancellationToken ct)
    {
        var command = new CreateUserCommand(
            request.UserName,
            request.Email,
            request.Password,
            request.PhoneNumber,
            request.FullName,
            request.BirthOfDate,
            (EnumGender)request.GenderId,
            request.CountryId,
            request.CityId,
            request.JobStatusId,
            roleName,
            profileImage);

        var result = await sender.Send(command, ct);
        return result.Match(
            dto => CreatedAtRoute("GetUserById", new { version = "1.0", userId = dto.UserId }, dto),
            Problem);
    }
}
