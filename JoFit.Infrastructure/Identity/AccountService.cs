using JoFit.Application.Common.Interfaces;
using JoFit.Domain.Careers;
using JoFit.Domain.Coaches;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Customers;
using JoFit.Domain.Identity.Sessions;
using JoFit.Domain.Identity.Users;
using JoFit.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace JoFit.Infrastructure.Identity;

public class AccountService(
    JoFitDbContext db,
    UserManager<User> userManager,
    ITokenProvider tokenProvider,
    IFileUploadService fileUploadService) : IAccountService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(14);

    public async Task<Result<SignedInDto>> SignInAsync(string email, string password, CancellationToken ct)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(normalized);

        // Unknown email and wrong password answer the same way so accounts cannot be enumerated.
        if (user is null || !user.IsActive)
        {
            return AccountErrors.InvalidCredentials;
        }

        var account = await ResolveAccountAsync(user, ct);
        if (account is null)
        {
            // A staff (or otherwise non-storefront) login: only revealed once the password is right.
            return await userManager.CheckPasswordAsync(user, password)
                ? AccountErrors.StaffAccount
                : AccountErrors.InvalidCredentials;
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            return AccountErrors.InvalidCredentials;
        }

        return await IssueAsync(user, account, ct);
    }

    public async Task<Result<SignedInDto>> RegisterCustomerAsync(RegisterCustomerInput input, CancellationToken ct)
    {
        var email = input.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return AccountErrors.EmailTaken;
        }

        if (input.ProfileImage is not null)
        {
            var imageError = fileUploadService.ValidateFile(input.ProfileImage, [".jpg", ".jpeg", ".png", ".webp"], 5);
            if (!string.IsNullOrEmpty(imageError))
            {
                return Error.Validation("User.InvalidProfileImage", imageError).ForField("profileImage");
            }
        }

        var user = new User
        {
            UserName = email,
            Email = email,
            PhoneNumber = input.Phone,
            EmailConfirmed = true,
            IsActive = true,
            FullName = input.FullName,
            BirthOfDate = input.BirthOfDate,
            GenderId = input.GenderId,
            CountryId = input.CountryId,
            CityId = input.CityId
        };

        var created = await userManager.CreateAsync(user, input.Password);
        if (!created.Succeeded)
        {
            return created.Errors
                .Select(e => Error.Validation($"Identity_{e.Code}", e.Description).ForField("password"))
                .ToList();
        }

        if (input.ProfileImage is not null)
        {
            var imageUrl = await fileUploadService.UploadFileAsync(input.ProfileImage, $"user/{user.Id}", ct);
            if (string.IsNullOrEmpty(imageUrl) || imageUrl.Length > 300)
            {
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    await fileUploadService.DeleteFileAsync(imageUrl, ct);
                }

                await userManager.DeleteAsync(user);
                return Error.Validation("User.InvalidProfileImage", "The profile image could not be saved. Use a shorter file name.").ForField("profileImage");
            }

            user.ProfileImageUrl = imageUrl;
            await userManager.UpdateAsync(user);
        }

        var customer = Customer.Create(user.Id, input.FullName, input.Phone, input.Locale, input.MarketingOptIn);
        db.Set<Customer>().Add(customer);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await userManager.DeleteAsync(user);
            throw;
        }

        var account = new AccountDto(user.Id, customer.Name, null, email, EnumAccountRole.Customer.ToRoleName(), CustomerId: customer.Id);
        return await IssueAsync(user, account, ct);
    }


    public async Task<Result<long>> CreateCoachLoginAsync(string email, string name, string password, CancellationToken ct)
    {
        var normalized = email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(normalized) is not null)
        {
            return AccountErrors.EmailTaken;
        }

        var user = new User
        {
            UserName = normalized,
            Email = normalized,
            EmailConfirmed = true,
            IsActive = true,
            FullName = name.Trim()
        };

        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            return created.Errors.Select(e => Error.Validation($"Identity_{e.Code}", e.Description).ForField("password")).ToList();
        }

        return user.Id;
    }

    public async Task<Result<Updated>> SetLoginEmailAsync(long userId, string email, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AccountErrors.NotSignedIn;
        }

        var normalized = email.Trim().ToLowerInvariant();
        if (string.Equals(user.Email, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Updated;
        }

        var existing = await userManager.FindByEmailAsync(normalized);
        if (existing is not null && existing.Id != user.Id)
        {
            return Error.Validation("Auth.EmailTaken", "Another account already uses that email address.").ForField("email");
        }

        user.Email = normalized;
        user.UserName = normalized;
        var updated = await userManager.UpdateAsync(user);
        return updated.Succeeded ? Result.Updated : AccountErrors.EmailTaken;
    }

    public async Task DeactivateAsync(long userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is not null)
        {
            user.IsActive = false;
            await userManager.UpdateAsync(user);
            await RevokeOtherSessionsAsync(userId, null, ct);
        }
    }

    public async Task<Result<SignedInDto>> RegisterEmployerAsync(RegisterEmployerInput input, CancellationToken ct)
    {
        var email = input.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return AccountErrors.EmailTaken;
        }

        var user = new User
        {
            UserName = email,
            Email = email,
            PhoneNumber = input.Phone,
            EmailConfirmed = true,
            IsActive = true,
            FullName = input.ContactName.Trim()
        };

        var created = await userManager.CreateAsync(user, input.Password);
        if (!created.Succeeded)
        {
            return created.Errors.Select(e => Error.Validation($"Identity_{e.Code}", e.Description).ForField("password")).ToList();
        }

        // The slug comes from the company name (latin letters), with -2, -3 on a clash, and "employer" as the fallback.
        var baseSlug = SlugOf(input.CompanyName);
        var taken = (await db.Set<Employer>().IgnoreQueryFilters().Select(e => e.Slug).ToListAsync(ct)).ToHashSet();
        var slug = baseSlug;
        for (var n = 2; taken.Contains(slug); n++) slug = $"{baseSlug}-{n}";

        var employer = Employer.Register(user.Id, slug, input.CompanyName, input.Kind, input.LocationId, input.ContactName, input.Website, input.Phone, email);
        db.Set<Employer>().Add(employer);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await userManager.DeleteAsync(user);
            throw;
        }

        var account = new AccountDto(user.Id, employer.Name, null, email, "employer", EmployerId: employer.Id);
        return await IssueAsync(user, account, ct);
    }

    private static string SlugOf(string text)
    {
        var builder = new System.Text.StringBuilder();
        var lastHyphen = true;
        foreach (var c in text.ToLowerInvariant())
        {
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9') { builder.Append(c); lastHyphen = false; }
            else if (!lastHyphen) { builder.Append('-'); lastHyphen = true; }
        }

        var slug = builder.ToString().Trim('-');
        return slug.Length == 0 ? "employer" : slug;
    }

    public async Task<Result<SessionDto>> GetSessionAsync(long userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        var account = user is null ? null : await ResolveAccountAsync(user, ct);
        return account is null ? AccountErrors.NotSignedIn : new SessionDto(account);
    }

    public async Task<Result<SignedInDto>> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return AccountErrors.NotSignedIn;
        }

        var hash = AuthTokens.Hash(refreshToken);
        var now = DateTimeOffset.UtcNow;
        var session = await db.Set<AccountSession>()
            .FirstOrDefaultAsync(s => s.Jti == hash && s.RevokedAtUtc == null && s.ExpiresAtUtc > now, ct);
        if (session is null)
        {
            return AccountErrors.NotSignedIn;
        }

        var user = await userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            session.Revoke();
            await db.SaveChangesAsync(ct);
            return AccountErrors.NotSignedIn;
        }

        // Staff sessions share the table; a staff refresh token has no storefront account and is not accepted here.
        var account = await ResolveAccountAsync(user, ct);
        if (account is null)
        {
            return AccountErrors.NotSignedIn;
        }

        var (newToken, newHash) = AuthTokens.NewRefreshToken();
        session.Rotate(newHash);
        await db.SaveChangesAsync(ct);

        return BuildSignedIn(user, account, session.Id, newToken);
    }


    public async Task RevokeSessionAsync(long? sessionId, string? refreshToken, CancellationToken ct)
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

    public async Task RevokeOtherSessionsAsync(long userId, long? keepSessionId, CancellationToken ct)
    {
        var sessions = await db.Set<AccountSession>()
            .Where(s => s.UserId == userId && s.RevokedAtUtc == null && s.Id != keepSessionId)
            .ToListAsync(ct);
        sessions.ForEach(s => s.Revoke());
        await db.SaveChangesAsync(ct);
    }

    public async Task<Result<Updated>> ChangePasswordAsync(
        long userId, long? keepSessionId, string currentPassword, string newPassword, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AccountErrors.NotSignedIn;
        }

        if (string.IsNullOrEmpty(currentPassword) || !await userManager.CheckPasswordAsync(user, currentPassword))
        {
            return AccountErrors.CurrentPasswordWrong;
        }

        var changed = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!changed.Succeeded)
        {
            return changed.Errors
                .Select(e => Error.Validation($"Identity_{e.Code}", e.Description).ForField("newPassword"))
                .ToList();
        }

        await RevokeOtherSessionsAsync(userId, keepSessionId, ct);
        return Result.Updated;
    }

    public async Task<Result<SessionDto>> ChangeEmailAsync(long userId, string email, string currentPassword, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AccountErrors.NotSignedIn;
        }

        var normalized = email.Trim().ToLowerInvariant();
        if (string.Equals(user.Email, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return AccountErrors.EmailSameAsCurrent;
        }

        if (string.IsNullOrEmpty(currentPassword) || !await userManager.CheckPasswordAsync(user, currentPassword))
        {
            return AccountErrors.CurrentPasswordWrong;
        }

        var existing = await userManager.FindByEmailAsync(normalized);
        if (existing is not null && existing.Id != user.Id)
        {
            return AccountErrors.EmailTaken;
        }

        user.Email = normalized;
        user.UserName = normalized;
        var updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            return AccountErrors.EmailTaken;
        }

        var account = await ResolveAccountAsync(user, ct);
        return account is null ? AccountErrors.NotSignedIn : new SessionDto(account);
    }

    /// <summary>
    /// The storefront role of a user, from the profile row they own. Null for staff and any other login.
    /// Coaches and employers are added here when their records exist.
    /// </summary>
    private async Task<AccountDto?> ResolveAccountAsync(User user, CancellationToken ct)
    {
        var coach = await db.Set<Coach>().AsNoTracking().FirstOrDefaultAsync(c => c.UserId == user.Id, ct);
        if (coach is not null)
        {
            return new AccountDto(user.Id, coach.NameEn, coach.NameAr, user.Email!, "coach", CoachId: coach.Id);
        }

        var employer = await db.Set<Employer>().AsNoTracking().FirstOrDefaultAsync(e => e.UserId == user.Id, ct);
        if (employer is not null)
        {
            return new AccountDto(user.Id, employer.Name, null, user.Email!, "employer", EmployerId: employer.Id);
        }

        var customer = await db.Set<Customer>().AsNoTracking().FirstOrDefaultAsync(c => c.UserId == user.Id, ct);
        return customer is null
            ? null
            : new AccountDto(user.Id, customer.Name, null, user.Email!, "customer", CustomerId: customer.Id);
    }

    private async Task<SignedInDto> IssueAsync(User user, AccountDto account, CancellationToken ct)
    {
        var (refreshToken, refreshHash) = AuthTokens.NewRefreshToken();
        var session = AccountSession.Start(user.Id, refreshHash, DateTimeOffset.UtcNow.Add(SessionLifetime));
        db.Set<AccountSession>().Add(session);
        await db.SaveChangesAsync(ct);

        return BuildSignedIn(user, account, session.Id, refreshToken);
    }

    /// <summary>A short-lived access token tied to its session. It is checked by signature and expiry only.</summary>
    private SignedInDto BuildSignedIn(User user, AccountDto account, long sessionId, string refreshToken)
    {
        var expires = DateTimeOffset.UtcNow.Add(AuthTokens.AccessLifetime);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(AuthTokens.SessionIdClaim, sessionId.ToString()),
            new(ClaimTypes.Role, account.Role),
            new("account_role", account.Role)
        };

        var token = tokenProvider.CreateAccessToken(claims, expires);
        return new SignedInDto(token, new SessionDto(account), refreshToken, expires);
    }
}

public static class AccountErrors
{
    public static readonly Error InvalidCredentials = Error.Unauthorized("Auth.InvalidCredentials", "The email or password is not right.");
    public static readonly Error StaffAccount = Error.Forbidden("Auth.StaffAccount", "Staff accounts sign in to the admin console.");
    public static readonly Error NotSignedIn = Error.Unauthorized("Auth.NotSignedIn", "Sign in to continue.");
    public static readonly Error EmailTaken = Error.Conflict("Auth.EmailTaken", "Another account already uses that email address.").ForField("email");
    public static readonly Error EmailSameAsCurrent = Error.Validation("Auth.EmailSameAsCurrent", "That is already your email address.").ForField("email");
    public static readonly Error CurrentPasswordWrong = Error.Validation("Auth.CurrentPasswordWrong", "The current password is not right.").ForField("currentPassword");
}
