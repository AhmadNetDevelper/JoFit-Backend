using JoFit.Application.Features.Identity;
using JoFit.Application.Features.Identity.Dtos;
using JoFit.Domain.Common.Results;
using System.Security.Claims;

namespace JoFit.Application.Common.Interfaces;

public interface ITokenProvider
{
    Task<Result<TokenResponse>> GenerateJwtTokenAsync(AppUserDto user, CancellationToken ct = default);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

    /// <summary>Signs a JWT with the shared issuer, audience and key. The one place access tokens are minted.</summary>
    string CreateAccessToken(IEnumerable<Claim> claims, DateTimeOffset expires);
}
