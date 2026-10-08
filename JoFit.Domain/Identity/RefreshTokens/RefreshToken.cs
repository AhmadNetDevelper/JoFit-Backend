using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Identity.RefreshTokens;

public class RefreshToken : AuditableEntity
{
    public string? Token { get; }
    public long UserId { get; }
    public DateTimeOffset ExpiresOnUtc { get; }

    private RefreshToken()
    { }

    private RefreshToken(long userId, string? token, DateTimeOffset expiresOnUtc)
        : base()
    {
        Token = token;
        UserId = userId;
        ExpiresOnUtc = expiresOnUtc;
    }

    public static Result<RefreshToken> Create(long userId, string? token, DateTimeOffset expiresOnUtc)
    {
        if (userId <= 0)
        {
            return RefreshTokenErrors.IdRequired;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return RefreshTokenErrors.TokenRequired;
        }

        if (expiresOnUtc <= DateTimeOffset.UtcNow)
        {
            return RefreshTokenErrors.ExpiryInvalid;
        }

        return new RefreshToken(userId, token, expiresOnUtc);
    }
}
