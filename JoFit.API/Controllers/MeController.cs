using Asp.Versioning;
using JoFit.Application.Features.Auth;
using JoFit.Application.Features.Customers;
using JoFit.Contracts.Requests.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

/// <summary>The signed-in customer's own records. The customer always comes from the token, never from the request.</summary>
[Route("api/v{version:apiVersion}/me")]
[ApiVersion("1.0")]
[Authorize(Roles = "customer")]
public sealed class MeController(ISender sender) : ApiController
{
    [HttpGet("profile")]
    [ProducesResponseType(typeof(CustomerProfileDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets the customer's profile.")]
    [EndpointName("GetMyProfile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var result = await sender.Send(new GetCustomerProfileQuery(CurrentUserId), ct);
        return result.Match(profile => Ok(profile), Problem);
    }

    [HttpPut("profile")]
    [ProducesResponseType(typeof(CustomerProfileDto), StatusCodes.Status200OK)]
    [EndpointSummary("Updates the customer's name and phone.")]
    [EndpointName("UpdateMyProfile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateCustomerProfileCommand(CurrentUserId, request.Name, request.Phone), ct);
        return result.Match(profile => Ok(profile), Problem);
    }

    [HttpGet("preferences")]
    [ProducesResponseType(typeof(CustomerPreferencesDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets the customer's preferences.")]
    [EndpointName("GetMyPreferences")]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        var result = await sender.Send(new GetCustomerPreferencesQuery(CurrentUserId), ct);
        return result.Match(preferences => Ok(preferences), Problem);
    }

    [HttpPut("preferences")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Saves the customer's preferences.")]
    [EndpointName("UpdateMyPreferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateCustomerPreferencesCommand(CurrentUserId, request.Locale, request.MarketingOptIn), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
