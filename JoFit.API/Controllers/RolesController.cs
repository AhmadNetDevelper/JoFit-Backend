using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.Application.Features.Roles.Commands.AssignPermissionsToRole;
using JoFit.Application.Features.Roles.Commands.CreateRole;
using JoFit.Application.Features.Roles.Commands.RemoveRole;
using JoFit.Application.Features.Roles.Commands.UpdatePermissionsFromRole;
using JoFit.Application.Features.Roles.Commands.UpdateRole;
using JoFit.Application.Features.Roles.Dtos;
using JoFit.Application.Features.Roles.Queries.GetRoleById;
using JoFit.Application.Features.Roles.Queries.GetRoles;
using JoFit.Contracts.Requests.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/roles")]
[ApiVersion("1.0")]
[JoFit.API.OpenApi.SharedApi]
public sealed class RolesController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Roles.Read)]
    [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves a list of roles.")]
    [EndpointName("GetRoles")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await sender.Send(new GetRolesQuery(), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("{roleId:long}/permissions", Name = "GetRolePermissions")]
    [Authorize(Permissions.Roles.Read)]
    [ProducesResponseType(typeof(RolePermissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieves permissions assigned to a role.")]
    [EndpointDescription("Returns the role together with all permissions assigned to it.")]
    public async Task<IActionResult> GetRolePermissions(long roleId, CancellationToken ct)
    {
        var result = await sender.Send(new GetRoleByIdQuery(roleId), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.Roles.Create)]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Creates a new role.")]
    [EndpointDescription("Adds a new role. Optionally clones the permissions of an existing role via CloneFromRoleId.")]
    [EndpointName("CreateRole")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateRoleCommand(request.Name, request.CloneFromRoleId), ct);
        return result.Match(
            dto => CreatedAtRoute("GetRolePermissions", new { version = "1.0", roleId = dto.RoleId }, dto),
            Problem);
    }

    [HttpPut("{roleId:long}")]
    [Authorize(Permissions.Roles.Update)]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Updates an existing role.")]
    [EndpointName("UpdateRole")]
    public async Task<IActionResult> UpdateRole(long roleId, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateRoleCommand(roleId, request.Name), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost("{roleId:long}/permissions")]
    [Authorize(Permissions.Roles.Update)]
    [ProducesResponseType(typeof(RolePermissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Assigns permissions to a role.")]
    [EndpointDescription("Adds one or more permissions to the specified role, keeping the ones already assigned.")]
    [EndpointName("AssignPermissionsToRole")]
    public async Task<IActionResult> AssignPermissionsToRole(long roleId, [FromBody] AssignPermissionsToRoleRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new AssignPermissionsToRoleCommand(roleId, request.PermissionIds), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPut("{roleId:long}/permissions")]
    [Authorize(Permissions.Roles.Update)]
    [ProducesResponseType(typeof(RolePermissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Updates permissions for a role.")]
    [EndpointDescription("Replaces the permissions of the role with the given list, adding new ones and removing the rest.")]
    [EndpointName("UpdatePermissionsFromRole")]
    public async Task<IActionResult> UpdatePermissionsFromRole(long roleId, [FromBody] UpdatePermissionsFromRoleRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdatePermissionsFromRoleCommand(roleId, request.PermissionIds), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpDelete("{roleId:long}")]
    [Authorize(Permissions.Roles.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Removes a role.")]
    [EndpointName("RemoveRole")]
    public async Task<IActionResult> Delete(long roleId, CancellationToken ct)
    {
        var result = await sender.Send(new RemoveRoleCommand(roleId), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
