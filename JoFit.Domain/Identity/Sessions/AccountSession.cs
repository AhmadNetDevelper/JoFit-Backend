using JoFit.Domain.Common;

namespace JoFit.Domain.Identity.Sessions;

/// <summary>
/// One row per sign-in (a refresh-token session). The short-lived access token is validated without touching
/// this table; the row is only read when the refresh token is exchanged for a new access token. Revoking a
/// session stops its refresh token, so the sign-in ends once the current access token expires.
/// </summary>
public class AccountSession : AuditableEntity
{
    public long UserId { get; private set; }

    /// <summary>The SHA-256 hash of the current refresh token (the token itself is never stored).</summary>
    public string Jti { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    private AccountSession()
    {
    }

    public static AccountSession Start(long userId, string refreshTokenHash, DateTimeOffset expiresAtUtc)
        => new() { UserId = userId, Jti = refreshTokenHash, ExpiresAtUtc = expiresAtUtc };

    /// <summary>Replaces the refresh token with a new one, so each refresh token works once.</summary>
    public void Rotate(string refreshTokenHash) => Jti = refreshTokenHash;

    public void Revoke() => RevokedAtUtc ??= DateTimeOffset.UtcNow;
}
