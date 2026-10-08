using Asp.Versioning;
using JoFit.API.Mapping;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Auth;
using JoFit.Application.Features.Users.Commands.CreateCoach;
using JoFit.Application.Features.Users.Commands.CreateUser;
using JoFit.Application.Features.Users.Dtos;
using JoFit.Contracts.Requests.Auth;
using JoFit.Contracts.Requests.Users;
using JoFit.Domain.Identity.Users;
using JoFit.Infrastructure.Data.Seeders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JoFit.API.Controllers;

/// <summary>Storefront sign-in for customers, coaches and employers.</summary>
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("1.0")]
public sealed class AuthController(ISender sender) : ApiController
{
    private const string StorefrontRoles = "customer,coach,employer";

    [HttpPost("sign-in")]
    [AllowAnonymous]
    [EnableRateLimiting("Auth")]
    [ProducesResponseType(typeof(SignedInDto), StatusCodes.Status200OK)]
    [EndpointSummary("Signs a customer, coach or employer in.")]
    [EndpointName("StorefrontSignIn")]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new SignInCommand(request.Email, request.Password), ct);
        return result.Match(signedIn => Ok(signedIn), Problem);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("Auth")]
    [ProducesResponseType(typeof(SignedInDto), StatusCodes.Status200OK)]
    [EndpointSummary("Creates a customer account and signs it in.")]
    [Consumes("multipart/form-data")]
    [EndpointName("RegisterCustomer")]
    public async Task<IActionResult> Register([FromForm] RegisterCustomerRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new RegisterCommand(
                request.FullName, request.Email, request.Phone, request.Password, request.Locale, request.MarketingOptIn,
                request.ProfileImage, request.BirthOfDate, (EnumGender)request.GenderId, request.CountryId, request.CityId), ct);
        return result.Match(signedIn => Ok(signedIn), Problem);
    }

    [HttpPost("register-coach")]
    [AllowAnonymous]
    [EnableRateLimiting("Auth")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Creates a coach.")]
    [EndpointDescription("Creates a user with the Coach role together with their educations, certificates, experiences and skills. Multipart form with these parts: User (login and personal data), Profile (the coach page: slug, name, headline, location, sportIds, languages, packages), Educations[i], Certificates[i] (optional CertificateFile), Experiences[i] and Skills[i]. Everything is saved in one transaction.")]
    [Consumes("multipart/form-data")]
    [EndpointName("RegisterCoach")]
    public async Task<IActionResult> RegisterCoach([FromForm] CreateCoachRequest request, CancellationToken ct)
    {
        var command = request.ToCommand();

        var result = await sender.Send(command, ct);
        return result.Match(dto => StatusCode(StatusCodes.Status201Created, dto), Problem);
    }

    [HttpPost("sign-out")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Ends this session. Send the refresh token; works without a valid access token.")]
    [EndpointName("StorefrontSignOut")]
    public async Task<IActionResult> SignOut(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] SignOutRequest? request,
        CancellationToken ct)
    {
        var result = await sender.Send(new SignOutCommand(CurrentSessionId, request?.RefreshToken), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("Auth")]
    [ProducesResponseType(typeof(SignedInDto), StatusCodes.Status200OK)]
    [EndpointSummary("Exchanges a refresh token for a new access token and refresh token.")]
    [EndpointName("RefreshStorefrontToken")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RefreshCommand(request.RefreshToken), ct);
        return result.Match(signedIn => Ok(signedIn), Problem);
    }

    [HttpGet("session")]
    [Authorize(Roles = StorefrontRoles)]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets the signed-in account.")]
    [EndpointName("GetStorefrontSession")]
    public async Task<IActionResult> GetSession(CancellationToken ct)
    {
        var result = await sender.Send(new GetSessionQuery(CurrentUserId), ct);
        return result.Match(session => Ok(session), Problem);
    }

    [HttpPut("password")]
    [Authorize(Roles = StorefrontRoles)]
    [EnableRateLimiting("Auth")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Changes the password and signs out every other device.")]
    [EndpointName("ChangePassword")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ChangePasswordCommand(CurrentUserId, CurrentSessionId, request.CurrentPassword, request.NewPassword), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPut("email")]
    [Authorize(Roles = StorefrontRoles)]
    [EnableRateLimiting("Auth")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [EndpointSummary("Changes the login email. Customers only.")]
    [EndpointName("ChangeEmail")]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ChangeEmailCommand(CurrentUserId, CurrentAccountRole, request.Email, request.CurrentPassword), ct);
        return result.Match(session => Ok(session), Problem);
    }

    [HttpPost("sessions/revoke-others")]
    [Authorize(Roles = StorefrontRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Signs out every other device.")]
    [EndpointName("RevokeOtherSessions")]
    public async Task<IActionResult> RevokeOthers(CancellationToken ct)
    {
        var result = await sender.Send(new RevokeOtherSessionsCommand(CurrentUserId, CurrentSessionId), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
