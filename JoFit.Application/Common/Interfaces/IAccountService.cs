using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity.Users;
using Microsoft.AspNetCore.Http;

namespace JoFit.Application.Common.Interfaces;

/// <summary>The signed-in account as the storefront reads it.</summary>
public sealed record AccountDto(
    long Id,
    string Name,
    string? NameAr,
    string Email,
    string Role,
    long? CustomerId = null,
    long? CoachId = null,
    long? EmployerId = null);

public sealed record SessionDto(AccountDto Account);

/// <summary>An access token (short-lived) and the refresh token that gets a new one. Both are returned on sign-in and refresh.</summary>
public sealed record SignedInDto(string Token, SessionDto Session, string RefreshToken, DateTimeOffset ExpiresAt);

/// <summary>An employer signing up: the company and its first login together.</summary>
public sealed record RegisterEmployerInput(
    string CompanyName, string Kind, long LocationId, string ContactName, string Email, string Phone, string? Website, string Password, string Locale);

public sealed record RegisterCustomerInput(string FullName, string Email, string? Phone, string Password, string Locale, bool MarketingOptIn,
    IFormFile? ProfileImage = null, DateTime? BirthOfDate = null, EnumGender GenderId = default, long? CountryId = null, long? CityId = null);

/// <summary>Storefront accounts (customers, coaches and employers) and their revocable sign-in sessions.</summary>
public interface IAccountService
{
    Task<Result<SignedInDto>> SignInAsync(string email, string password, CancellationToken ct);

    Task<Result<SignedInDto>> RegisterCustomerAsync(RegisterCustomerInput input, CancellationToken ct);

    /// <summary>Opens an employer account and its company (pending verification) and signs it in.</summary>
    Task<Result<SignedInDto>> RegisterEmployerAsync(RegisterEmployerInput input, CancellationToken ct);

    Task<Result<SessionDto>> GetSessionAsync(long userId, CancellationToken ct);

    /// <summary>Exchanges a refresh token for a new access token and a new refresh token (the old one stops working).</summary>
    Task<Result<SignedInDto>> RefreshAsync(string refreshToken, CancellationToken ct);

    /// <summary>Revokes the session by its id (from the access token) or by its refresh token. No error when unknown.</summary>
    Task RevokeSessionAsync(long? sessionId, string? refreshToken, CancellationToken ct);

    /// <summary>Revokes every session of the user except <paramref name="keepSessionId"/>.</summary>
    Task RevokeOtherSessionsAsync(long userId, long? keepSessionId, CancellationToken ct);

    /// <summary>Verifies the current password, sets the new one, and revokes every other session.</summary>
    Task<Result<Updated>> ChangePasswordAsync(long userId, long? keepSessionId, string currentPassword, string newPassword, CancellationToken ct);

    /// <summary>Creates the login of a coach (the coach record links to it). The email must be unused by any account.</summary>
    Task<Result<long>> CreateCoachLoginAsync(string email, string name, string password, CancellationToken ct);

    /// <summary>Sets a login email without the password check (staff editing a customer). A duplicate is a 422 on email.</summary>
    Task<Result<Updated>> SetLoginEmailAsync(long userId, string email, CancellationToken ct);

    /// <summary>Switches a login off and ends its sessions (for example when its coach is deleted).</summary>
    Task DeactivateAsync(long userId, CancellationToken ct);

    /// <summary>Customers only. Verifies the password, then updates the login email and the customer record.</summary>
    Task<Result<SessionDto>> ChangeEmailAsync(long userId, string email, string currentPassword, CancellationToken ct);
}
