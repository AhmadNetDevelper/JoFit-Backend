using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.Application.Features.Permissions.Dtos;
using JoFit.Application.Features.Permissions.Queries.GetPermissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/permissions")]
[ApiVersion("1.0")]
[JoFit.API.OpenApi.SharedApi]
public sealed class PermissionsController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Roles.Read)]
    [ProducesResponseType(typeof(List<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves a list of all permissions.")]
    [EndpointDescription("Returns all permissions available in the system.")]
    [EndpointName("GetAllPermissions")]
    public async Task<IActionResult> GetAllPermissions(CancellationToken ct)
    {
        var result = await sender.Send(new GetPermissionsQuery(), ct);
        return result.Match(response => Ok(response), Problem);
    }
}
