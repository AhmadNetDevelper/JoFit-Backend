using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace JoFit.Infrastructure.Identity;

/// <summary>Shared rules for the access token (short, stateless) and the refresh token (opaque, stored hashed).</summary>
internal static class AuthTokens
{
    /// <summary>How long an access token is accepted. Revoking a session takes effect within this time.</summary>
    public static readonly TimeSpan AccessLifetime = TimeSpan.FromMinutes(15);

    /// <summary>The claim that names the refresh-token session an access token came from.</summary>
    public const string SessionIdClaim = "session_id";

    /// <summary>A new random refresh token and the hash that is stored for it.</summary>
    public static (string Token, string Hash) NewRefreshToken()
    {
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return (token, Hash(token));
    }

    public static string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
