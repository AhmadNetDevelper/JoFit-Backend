using JoFit.Application.Common.Models;
using JoFit.Domain.Common.Results;

namespace JoFit.Application.Common.Interfaces;

public sealed record AdminUserDto(long Id, string Name, string Email, string Role);

/// <summary>What the admin needs after signing in: who they are and what they may do ("catalogue:write", ...).</summary>
public sealed record AdminSessionDto(AdminUserDto User, IReadOnlyList<string> Permissions);

/// <summary>The access token and when it expires, plus the refresh token that renews it and when that stops working.</summary>
public sealed record StaffSignedInDto(
    AdminSessionDto Session, string Token, DateTimeOffset ExpiresAt, string RefreshToken, DateTimeOffset RefreshExpiresAt);

/// <summary>A created staff member. TemporaryPassword is shown once, when no password was supplied.</summary>
public sealed record AdminUserCreatedDto(long Id, string Name, string Email, string Role, string? TemporaryPassword);

public sealed record StaffInput(string Name, string Email, string Role, string? Password);

/// <summary>Back-office staff: their sign-in, roles and team management.</summary>
public interface IStaffService
{
    Task<Result<StaffSignedInDto>> SignInAsync(string email, string password, CancellationToken ct);

    /// <summary>Exchanges a refresh token for a new access token and a new refresh token (the old one stops working).</summary>
    Task<Result<StaffSignedInDto>> RefreshAsync(string refreshToken, CancellationToken ct);

    /// <summary>Revokes the session by its id (from the access token) or by its refresh token. No error when unknown.</summary>
    Task SignOutAsync(long? sessionId, string? refreshToken, CancellationToken ct);

    Task<Result<AdminSessionDto>> GetSessionAsync(long userId, CancellationToken ct);

    Task<PagedResult<AdminUserDto>> ListAsync(int page, int pageSize, string? search, string? sortField, string? sortDir, CancellationToken ct);

    Task<List<AdminUserDto>> OptionsAsync(CancellationToken ct);

    Task<Result<AdminUserCreatedDto>> CreateAsync(StaffInput input, CancellationToken ct);

    Task<Result<AdminUserDto>> UpdateAsync(long id, StaffInput input, CancellationToken ct);

    Task<Result<Deleted>> DeleteAsync(long id, long? currentUserId, CancellationToken ct);
}
