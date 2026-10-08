using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Staff;
using JoFit.Contracts.Requests.Auth;
using JoFit.Contracts.Requests.Staff;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JoFit.API.Controllers;

/// <summary>The same token pair as Identity/token/generate, plus the staff member's permissions.</summary>
public sealed record AdminTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresOnUtc,
    IReadOnlyList<string> Permissions)
{
    public static AdminTokenResponse From(StaffSignedInDto signedIn)
        => new(signedIn.Token, signedIn.RefreshToken, signedIn.ExpiresAt.UtcDateTime, signedIn.Session.Permissions);
}

/// <summary>Back-office sign-in. The tokens are returned in the body; the admin sends the access token as a Bearer header.</summary>
[Route("api/v{version:apiVersion}/admin/auth")]
[ApiVersion("1.0")]
public sealed class AdminAuthController(ISender sender) : ApiController
{
    [HttpPost("sign-in")]
    [AllowAnonymous]
    [EnableRateLimiting("Auth")]
    [ProducesResponseType(typeof(AdminTokenResponse), StatusCodes.Status200OK)]
    [EndpointSummary("Signs a staff member in and returns the access token, refresh token and permissions.")]
    [EndpointName("AdminSignIn")]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new AdminSignInCommand(request.Email, request.Password), ct);
        return result.Match(signedIn => Ok(AdminTokenResponse.From(signedIn)), Problem);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("Auth")]
    [ProducesResponseType(typeof(AdminTokenResponse), StatusCodes.Status200OK)]
    [EndpointSummary("Exchanges a refresh token for a new access token and refresh token.")]
    [EndpointName("AdminRefresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new AdminRefreshCommand(request.RefreshToken), ct);
        return result.Match(signedIn => Ok(AdminTokenResponse.From(signedIn)), Problem);
    }


    [HttpPost("sign-out")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Ends the session. Send the refresh token; works without a valid access token.")]
    [EndpointName("AdminSignOut")]
    public async Task<IActionResult> SignOut(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] SignOutRequest? request,
        CancellationToken ct)
    {
        // Revokes the session, so the refresh token stops working and the access token lapses within minutes.
        await sender.Send(new AdminSignOutCommand(CurrentSessionId, request?.RefreshToken), ct);
        return NoContent();
    }

    [HttpGet("session")]
    [Authorize(Policy = "AnyStaff")]
    [ProducesResponseType(typeof(AdminSessionDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets the signed-in staff member and their permissions.")]
    [EndpointName("GetAdminSession")]
    public async Task<IActionResult> GetSession(CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminSessionQuery(CurrentUserId), ct);
        return result.Match(session => Ok(session), Problem);
    }
}

[Route("api/v{version:apiVersion}/admin/staff")]
[ApiVersion("1.0")]
public sealed class AdminStaffController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Policy = "AnyStaff")]
    [ProducesResponseType(typeof(PagedResult<AdminUserDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of staff.")]
    [EndpointName("GetAdminStaff")]
    public async Task<IActionResult> GetPaged([FromQuery] GetStaffPagedRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetStaffPagedQuery(request.Page, request.PageSize, request.Search, request.SortField, request.SortDir), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("options")]
    [Authorize(Policy = "AnyStaff")]
    [ProducesResponseType(typeof(List<AdminUserDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets every staff member for pickers.")]
    [EndpointName("GetAdminStaffOptions")]
    public async Task<IActionResult> GetOptions(CancellationToken ct)
    {
        var result = await sender.Send(new GetStaffOptionsQuery(), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.Settings.Write)]
    [ProducesResponseType(typeof(AdminUserCreatedDto), StatusCodes.Status201Created)]
    [EndpointSummary("Adds a staff member. Without a password a temporary one is returned once.")]
    [EndpointName("CreateStaff")]
    public async Task<IActionResult> Create([FromBody] StaffDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateStaffCommand(request.Name, request.Email, request.Role, request.Password), ct);
        return result.Match(dto => StatusCode(StatusCodes.Status201Created, dto), Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.Settings.Write)]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [EndpointSummary("Updates a staff member's name, email and role.")]
    [EndpointName("UpdateStaff")]
    public async Task<IActionResult> Update(long id, [FromBody] StaffDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateStaffCommand(id, request.Name, request.Email, request.Role), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.Settings.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Removes a staff member.")]
    [EndpointName("DeleteStaff")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteStaffCommand(id, CurrentUserId), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
