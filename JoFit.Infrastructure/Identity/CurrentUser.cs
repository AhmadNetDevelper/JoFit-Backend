using JoFit.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace JoFit.Infrastructure.Identity;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : IUser
{
    public long? Id
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(claim, out var id) ? id : null;
        }
    }

    public string? TokenId => httpContextAccessor.HttpContext?.User?.FindFirstValue("jti");

    public string? AccountRole => httpContextAccessor.HttpContext?.User?.FindFirstValue("account_role");

    public string? Language
    {
        get
        {
            // Priority: ?language= query string, then Language claim, then Accept-Language header
            var httpContext = httpContextAccessor.HttpContext;
            var languageClaim = httpContext?.Request.Query["language"].FirstOrDefault()
                ?? httpContext?.Request.Query["locale"].FirstOrDefault()
                ?? httpContext?.User?.FindFirstValue("Language")
                ?? httpContext?.Request.Headers["Accept-Language"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(languageClaim))
                return null;

            return languageClaim
                .Split(['-', ','], StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()
                ?.ToLowerInvariant();
        }
    }
}
