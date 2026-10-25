using Asp.Versioning;
using JoFit.Application.Features.Coaches;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

/// <summary>The cities a coach can be based in, for the public coach sign-up form. Reference data only.</summary>
[Route("api/v{version:apiVersion}/coach-locations")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class PublicCoachLocationsController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<LocationDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Lists the locations a coach can be based in.")]
    [EndpointDescription("Each location carries its name in both languages (`name.en`, `name.ar`).")]
    [EndpointName("GetPublicCoachLocations")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetCoachLocationsQuery(), ct);
        return result.Match(items => Ok(items), Problem);
    }
}
