namespace JoFit.Application.Common.Interfaces;

public interface IUser
{
    long? Id { get; }
    string? Language { get; }

    /// <summary>The JWT id (jti) of the token making the request, if any.</summary>
    string? TokenId { get; }

    /// <summary>The storefront role claim (customer, coach or employer), if the token carries one.</summary>
    string? AccountRole { get; }
}
