using Asp.Versioning;
using JoFit.Application.Features.Identity;
using JoFit.Application.Features.Identity.Dtos;
using JoFit.Application.Features.Identity.Queries.GetUserInfo;
using JoFit.Application.Features.Identity.Queries.Login;
using JoFit.Application.Features.Identity.Queries.RefreshTokens;
using JoFit.Application.Common.Interfaces;
using JoFit.Contracts.Requests.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JoFit.API.Controllers
{
    [Route("api/[controller]")]
    [ApiVersionNeutral]
    public sealed class IdentityController(ISender sender, IIdentityService identityService) : ApiController
    {
        [HttpPost("token/generate")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [EndpointSummary("Generates an access and refresh token for a valid user.")]
        [EndpointDescription("Authenticates a user using provided credentials and returns a JWT token pair.")]
        [EndpointName("GenerateToken")]
        public async Task<IActionResult> Login([FromBody] LoginQuery request, CancellationToken ct)
        {
            var result = await sender.Send(request, ct);
            return result.Match(
                response => Ok(response),
                Problem);
        }

            [HttpPost("register")]
            [AllowAnonymous]
            [ProducesResponseType(typeof(AppUserDto), StatusCodes.Status201Created)]
            [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
            public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
            {
                var result = await identityService.RegisterAsync(request, ct);
                return result.Match(
                response => StatusCode(StatusCodes.Status201Created, response),
                Problem);
            }

        [HttpPost("token/refresh-token")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [EndpointSummary("Refreshes access token using a valid refresh token.")]
        [EndpointDescription("Exchanges an expired access token and a valid refresh token for a new token pair.")]
        [EndpointName("RefreshToken")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenQuery request, CancellationToken ct)
        {
            var result = await sender.Send(request, ct);
            return result.Match(
                response => Ok(response),
                Problem);
        }

        [HttpGet("current-user/user-permissions")]
        [Authorize]
        [ProducesResponseType(typeof(AppUserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [EndpointSummary("Gets the current authenticated user's info.")]
        [EndpointDescription("Returns user information for the currently authenticated user based on the access token.")]
        [EndpointName("GetCurrentUserPermissions")]
        public async Task<IActionResult> GetCurrentUserPermissions(CancellationToken ct)
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var result = await sender.Send(new GetUserByIdQuery(userId), ct);

            return result.Match(
                response => Ok(response),
                Problem);
        }
    }
}
