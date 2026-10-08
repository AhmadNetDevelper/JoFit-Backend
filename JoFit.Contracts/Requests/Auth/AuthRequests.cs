using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace JoFit.Contracts.Requests.Auth;

public sealed record SignInRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed record RefreshRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

/// <summary>Body for sign-out: the refresh token to end, so it works even after the access token has expired.</summary>
public sealed record SignOutRequest
{
    public string? RefreshToken { get; init; }
}

public sealed record RegisterCustomerRequest
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string Password { get; init; } = string.Empty;
    public string Locale { get; init; } = "ar";
    public bool MarketingOptIn { get; init; }
    public IFormFile? ProfileImage { get; init; }
    public DateTime? BirthOfDate { get; init; }

    /// <summary>1 = Male, 2 = Female.</summary>
    [Range(1, 2)]
    public short GenderId { get; init; }

    public long? CountryId { get; init; }

    public long? CityId { get; init; }
}

public sealed record ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

public sealed record ChangeEmailRequest
{
    public string Email { get; init; } = string.Empty;
    public string CurrentPassword { get; init; } = string.Empty;
}

public sealed record UpdateProfileRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
}

public sealed record UpdatePreferencesRequest
{
    public string Locale { get; init; } = "en";
    public bool MarketingOptIn { get; init; }
}
