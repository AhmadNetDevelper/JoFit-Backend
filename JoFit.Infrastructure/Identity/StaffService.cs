using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity;
using JoFit.Domain.Identity.Roles;
using JoFit.Domain.Identity.Sessions;
using JoFit.Domain.Identity.Users;
using JoFit.Infrastructure.Data;
using JoFit.Infrastructure.Data.Seeders;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace JoFit.Infrastructure.Identity;

public class StaffService(
    JoFitDbContext db,
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    ITokenProvider tokenProvider) : IStaffService
{
    /// <summary>An admin sign-in lasts half a day; the refresh token stops working then and the admin signs in again.</summary>
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);

    public async Task<Result<StaffSignedInDto>> SignInAsync(string email, string password, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email.Trim().ToLowerInvariant());
        if (user is null || !user.IsActive)
        {
            return StaffErrors.InvalidCredentials;
        }

        var role = await StaffRoleOfAsync(user);
        if (role is null)
        {
            return StaffErrors.InvalidCredentials;
        }

        // Lockout only counts attempts against real staff accounts, so it cannot be used to lock out anyone else.
        if (!await userManager.GetLockoutEnabledAsync(user))
        {
            await userManager.SetLockoutEnabledAsync(user, true);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return StaffErrors.LockedOut;
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);
            return await userManager.IsLockedOutAsync(user) ? StaffErrors.LockedOut : StaffErrors.InvalidCredentials;
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var (refreshToken, refreshHash) = AuthTokens.NewRefreshToken();
        var refreshExpires = DateTimeOffset.UtcNow.Add(SessionLifetime);
        var staffSession = AccountSession.Start(user.Id, refreshHash, refreshExpires);
        db.Set<AccountSession>().Add(staffSession);
        await db.SaveChangesAsync(ct);

        return await BuildSignedInAsync(user, role, staffSession.Id, refreshToken, refreshExpires, ct);
    }

    public async Task<Result<StaffSignedInDto>> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return StaffErrors.NotSignedIn;
        }

        var hash = AuthTokens.Hash(refreshToken);
        var now = DateTimeOffset.UtcNow;
        var staffSession = await db.Set<AccountSession>()
            .FirstOrDefaultAsync(s => s.Jti == hash && s.RevokedAtUtc == null && s.ExpiresAtUtc > now, ct);
        if (staffSession is null)
        {
            return StaffErrors.NotSignedIn;
        }

        // The role is read again here, so a demotion or deactivation takes effect when the access token is renewed.
        var user = await userManager.FindByIdAsync(staffSession.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            staffSession.Revoke();
            await db.SaveChangesAsync(ct);
            return StaffErrors.NotSignedIn;
        }

        // Storefront sessions share the table; a storefront refresh token is not accepted here.
        var role = await StaffRoleOfAsync(user);
        if (role is null)
        {
            return StaffErrors.NotSignedIn;
        }

        var (newToken, newHash) = AuthTokens.NewRefreshToken();
        staffSession.Rotate(newHash);
        await db.SaveChangesAsync(ct);

        return await BuildSignedInAsync(user, role, staffSession.Id, newToken, staffSession.ExpiresAtUtc, ct);
    }


    public async Task SignOutAsync(long? sessionId, string? refreshToken, CancellationToken ct)
    {
        var hash = string.IsNullOrWhiteSpace(refreshToken) ? null : AuthTokens.Hash(refreshToken);
        if (sessionId is null && hash is null)
        {
            return;
        }

        var sessions = await db.Set<AccountSession>()
            .Where(s => s.RevokedAtUtc == null && ((sessionId != null && s.Id == sessionId) || (hash != null && s.Jti == hash)))
            .ToListAsync(ct);
        sessions.ForEach(s => s.Revoke());
        await db.SaveChangesAsync(ct);
    }

    private async Task<StaffSignedInDto> BuildSignedInAsync(
        User user, string role, long sessionId, string refreshToken, DateTimeOffset refreshExpires, CancellationToken ct)
    {
        var session = await BuildSessionAsync(user, role, ct);
        var expires = DateTimeOffset.UtcNow.Add(AuthTokens.AccessLifetime);
        return new StaffSignedInDto(session, CreateToken(user, role, sessionId, expires), expires, refreshToken, refreshExpires);
    }

    private async Task RevokeSessionsAsync(long userId, CancellationToken ct)
    {
        var sessions = await db.Set<AccountSession>().Where(s => s.UserId == userId && s.RevokedAtUtc == null).ToListAsync(ct);
        sessions.ForEach(s => s.Revoke());
        await db.SaveChangesAsync(ct);
    }

    public async Task<Result<AdminSessionDto>> GetSessionAsync(long userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        var role = user is null ? null : await StaffRoleOfAsync(user);
        return user is null || role is null ? StaffErrors.NotSignedIn : await BuildSessionAsync(user, role, ct);
    }

    public async Task<PagedResult<AdminUserDto>> ListAsync(
        int page, int pageSize, string? search, string? sortField, string? sortDir, CancellationToken ct)
    {
        var rows = StaffQuery();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            rows = rows.Where(x => x.User.FullName.ToLower().Contains(term)
                                   || x.User.Email!.ToLower().Contains(term)
                                   || x.RoleName.ToLower().Contains(term));
        }

        var descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        rows = sortField?.ToLowerInvariant() switch
        {
            "email" => descending ? rows.OrderByDescending(x => x.User.Email) : rows.OrderBy(x => x.User.Email),
            "role" => descending ? rows.OrderByDescending(x => x.RoleName) : rows.OrderBy(x => x.RoleName),
            _ => descending
                ? rows.OrderByDescending(x => x.User.FullName)
                : rows.OrderBy(x => x.User.FullName)
        };

        var paged = await rows.ToPagedResultAsync(page, pageSize, ct);
        return new PagedResult<AdminUserDto>(paged.Items.Select(ToDto).ToList(), paged.Page, paged.PageSize, paged.TotalCount);
    }

    public async Task<List<AdminUserDto>> OptionsAsync(CancellationToken ct)
        => (await StaffQuery().OrderBy(x => x.User.FullName).ToListAsync(ct)).Select(ToDto).ToList();

    public async Task<Result<AdminUserCreatedDto>> CreateAsync(StaffInput input, CancellationToken ct)
    {
        var email = input.Email.ToLowerInvariant();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return StaffErrors.EmailTaken;
        }

        var user = new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            IsActive = true,
            FullName = input.Name.Trim()
        };

        // No password on the draft: a random one is set and shown once, so the owner can hand it over.
        var generated = string.IsNullOrEmpty(input.Password);
        var password = generated ? $"Tmp-{Guid.NewGuid():N}"[..16] + "!a1" : input.Password!;

        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            return created.Errors.Select(e => Error.Validation($"Identity_{e.Code}", e.Description).ForField("email")).ToList();
        }

        await userManager.AddToRoleAsync(user, input.Role);
        return new AdminUserCreatedDto(user.Id, input.Name, email, input.Role, generated ? password : null);
    }

    public async Task<Result<AdminUserDto>> UpdateAsync(long id, StaffInput input, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        var currentRole = user is null ? null : await StaffRoleOfAsync(user);
        if (user is null || currentRole is null)
        {
            return StaffErrors.NotFound;
        }

        var email = input.Email.ToLowerInvariant();
        var other = await userManager.FindByEmailAsync(email);
        if (other is not null && other.Id != user.Id)
        {
            return StaffErrors.EmailTaken;
        }

        if (currentRole == Role.Owner && input.Role != Role.Owner && await CountOwnersAsync(ct) <= 1)
        {
            return StaffErrors.LastOwner;
        }

        user.FullName = input.Name.Trim();
        user.Email = email;
        user.UserName = email;
        var updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            return StaffErrors.EmailTaken;
        }

        if (currentRole != input.Role)
        {
            var roles = await userManager.GetRolesAsync(user);
            await userManager.RemoveFromRolesAsync(user, roles.Where(r => Role.StaffRolesIncludingSuperAdmin.Contains(r)));
            await userManager.AddToRoleAsync(user, input.Role);
            await RevokeSessionsAsync(user.Id, ct);
        }

        return new AdminUserDto(user.Id, input.Name, email, input.Role);
    }

    public async Task<Result<Deleted>> DeleteAsync(long id, long? currentUserId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        var role = user is null ? null : await StaffRoleOfAsync(user);
        if (user is null || role is null)
        {
            return StaffErrors.NotFound;
        }

        if (currentUserId == id)
        {
            return StaffErrors.DeleteSelf;
        }

        if (role == Role.Owner && await CountOwnersAsync(ct) <= 1)
        {
            return StaffErrors.LastOwner;
        }

        await RevokeSessionsAsync(user.Id, ct);
        await userManager.DeleteAsync(user);
        return Result.Deleted;
    }

    // ---- helpers ------------------------------------------------------------

    /// <summary>The admin role of a user: one of the six, superAdmin counting as owner. Null for non-staff.</summary>
    private async Task<string?> StaffRoleOfAsync(User user)
    {
        var roles = await userManager.GetRolesAsync(user);
        if (roles.Contains(Role.SuperAdmin) || roles.Contains(Role.Owner))
        {
            return Role.Owner;
        }

        return Role.StaffRoles.FirstOrDefault(roles.Contains);
    }

    private IQueryable<StaffRow> StaffQuery()
        => from ur in db.Set<IdentityUserRole<long>>()
           join r in db.Set<Role>() on ur.RoleId equals r.Id
           join u in db.Set<User>() on ur.UserId equals u.Id
           where Role.StaffRolesIncludingSuperAdmin.Contains(r.Name!)
           select new StaffRow { User = u, RoleName = r.Name == Role.SuperAdmin ? Role.Owner : r.Name! };

    private static AdminUserDto ToDto(StaffRow row)
        => new(row.User.Id, DisplayName(row.User), row.User.Email!, row.RoleName);

    private static string DisplayName(User user)
    {
        var name = user.FullName.Trim();
        return name.Length > 0 ? name : user.UserName ?? user.Email ?? string.Empty;
    }

    private async Task<int> CountOwnersAsync(CancellationToken ct)
        => await StaffQuery().CountAsync(x => x.RoleName == Role.Owner, ct);

    /// <summary>The user's permissions in the admin's "resource:action" form, e.g. catalogue:write.</summary>
    private async Task<AdminSessionDto> BuildSessionAsync(User user, string role, CancellationToken ct)
    {
        var roleIds = await db.Set<IdentityUserRole<long>>().Where(ur => ur.UserId == user.Id).Select(ur => ur.RoleId).ToListAsync(ct);
        var denied = await db.Set<UserPermission>().Where(up => up.UserId == user.Id && !up.IsGranted).Select(up => up.PermissionId).ToListAsync(ct);
        var granted = await db.Set<UserPermission>().Where(up => up.UserId == user.Id && up.IsGranted).Select(up => new { up.Permission.Resource, up.Permission.Action }).ToListAsync(ct);
        var viaRoles = await db.Set<RolePermission>()
            .Where(rp => roleIds.Contains(rp.RoleId) && !denied.Contains(rp.PermissionId))
            .Select(rp => new { rp.Permission.Resource, rp.Permission.Action })
            .ToListAsync(ct);

        var permissions = granted.Concat(viaRoles)
            .Select(p => $"{p.Resource.ToLowerInvariant()}:{p.Action.ToLowerInvariant()}")
            .Distinct()
            .Order()
            .ToList();

        return new AdminSessionDto(
            new AdminUserDto(user.Id, DisplayName(user), user.Email!, role),
            permissions);
    }

    private string CreateToken(User user, string role, long sessionId, DateTimeOffset expires)
    {
        return tokenProvider.CreateAccessToken(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(AuthTokens.SessionIdClaim, sessionId.ToString()),
            new Claim(ClaimTypes.Role, role)
        ], expires);
    }

    private sealed class StaffRow
    {
        public User User { get; init; } = null!;
        public string RoleName { get; init; } = string.Empty;
    }
}

public static class StaffErrors
{
    public static readonly Error InvalidCredentials = Error.Unauthorized("Staff.InvalidCredentials", "The email or password is not right.");
    public static readonly Error LockedOut = Error.Unauthorized("Staff.LockedOut", "Too many failed attempts. Try again in a few minutes.");
    public static readonly Error NotSignedIn =Error.Unauthorized("Staff.NotSignedIn", "Your session has ended. Sign in again.");
    public static readonly Error NotFound = Error.NotFound("Staff.NotFound", "That staff member does not exist.");
    public static readonly Error EmailTaken = Error.Validation("Staff.EmailTaken", "Another account already uses that email address.").ForField("email");
    public static readonly Error LastOwner = Error.Conflict("Staff.LastOwner", "This is the only owner. Make someone else an owner first.");
    public static readonly Error DeleteSelf = Error.Conflict("Staff.DeleteSelf", "You cannot delete your own account.");
}
