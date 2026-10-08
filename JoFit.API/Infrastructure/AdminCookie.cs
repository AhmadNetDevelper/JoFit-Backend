using JoFit.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace JoFit.API.Infrastructure;

/// <summary>
/// The httpOnly cookies that carry the staff tokens. The admin app never sees the tokens themselves.
/// The access cookie is short-lived and sent everywhere; the refresh cookie lives for the whole sign-in
/// but is only sent to the admin auth endpoints, which swap it for a new access cookie.
/// </summary>
public static class AdminCookie
{
    public const string Name = "jofit.admin";
    public const string RefreshName = "jofit.admin.refresh";

    private const string RefreshPath = "/api/v1/admin/auth";

    public static void Write(HttpResponse response, HttpRequest request, string token, DateTimeOffset expires)
        => response.Cookies.Append(Name, token, Options(request, expires, "/"));

    public static void WriteRefresh(HttpResponse response, HttpRequest request, string token, DateTimeOffset expires)
        => response.Cookies.Append(RefreshName, token, Options(request, expires, RefreshPath));

    public static void Clear(HttpResponse response, HttpRequest request)
    {
        response.Cookies.Append(Name, string.Empty, Options(request, DateTimeOffset.UnixEpoch, "/"));
        response.Cookies.Append(RefreshName, string.Empty, Options(request, DateTimeOffset.UnixEpoch, RefreshPath));
    }

    private static CookieOptions Options(HttpRequest request, DateTimeOffset expires, string path) => new()
    {
        HttpOnly = true,
        Secure = request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Path = path,
        Expires = expires,
        MaxAge = expires > DateTimeOffset.UtcNow ? expires - DateTimeOffset.UtcNow : TimeSpan.Zero
    };
}

/// <summary>
/// CSRF guard for cookie-authenticated writes: a request that relies on the admin cookie (no Authorization
/// header) must come from an allowed origin. SameSite=Lax already blocks cross-site form posts; this closes the rest.
/// </summary>
public sealed class AdminCsrfMiddleware(RequestDelegate next, IOptions<AppSettings> settings)
{
    private static readonly HashSet<string> SafeMethods = ["GET", "HEAD", "OPTIONS", "TRACE"];

    public async Task InvokeAsync(HttpContext context)
    {
        var usesCookie = (context.Request.Cookies.ContainsKey(AdminCookie.Name) || context.Request.Cookies.ContainsKey(AdminCookie.RefreshName))
                         && !context.Request.Headers.ContainsKey("Authorization");
        // Sign-in authenticates with the credentials in its body, not the cookie, so a stale cookie left in the
        // browser (cookies are shared across ports on localhost) must not block it.
        var isSignIn = context.Request.Path.Value?.EndsWith("/admin/auth/sign-in", StringComparison.OrdinalIgnoreCase) == true;
        if (usesCookie && !isSignIn && !SafeMethods.Contains(context.Request.Method))
        {
            var origin = context.Request.Headers.Origin.ToString();
            if (origin.Length > 0 && !settings.Value.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\":{\"code\":\"forbidden\",\"message\":\"That request came from an unknown origin.\"}}");
                return;
            }
        }

        await next(context);
    }
}
