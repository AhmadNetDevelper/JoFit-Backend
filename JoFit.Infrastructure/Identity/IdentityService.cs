using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Identity.Dtos;
using JoFit.Contracts.Requests.Common;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity;
using JoFit.Domain.Identity.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JoFit.Infrastructure.Identity;

public class IdentityService(ILogger<IdentityService> logger,
    IUnitOfWork unitOfWork,
    UserManager<User> userManager,
    IUserClaimsPrincipalFactory<User> userClaimsPrincipalFactory,
    IAuthorizationService authorizationService) : IIdentityService
{
    private readonly ILogger<IdentityService> _logger = logger;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly UserManager<User> _userManager = userManager;
    private readonly IUserClaimsPrincipalFactory<User> _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
    private readonly IAuthorizationService _authorizationService = authorizationService;

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<Result<AppUserDto>> GetUserByIdAsync(long userId, CancellationToken ct)
    {
        var dbContext = _unitOfWork.GetDbContext();

        var user = await dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException(nameof(userId));

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetUserPermissionsAsync(user.Id, ct);

        return new AppUserDto(
            user.Id,
            user.Email!,
            roles,
            permissions);
    }

    public async Task<Result<AppUserDto>> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var user = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .Select(error => Error.Validation($"Identity_{error.Code}", error.Description))
                .ToList();

            return errors;
        }

        return new AppUserDto(user.Id, user.Email!, []);
    }

    public async Task<bool> AuthorizeAsync(string userId, string? policyName)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return false;

        var principal = await _userClaimsPrincipalFactory.CreateAsync(user);
        var result = await _authorizationService.AuthorizeAsync(principal, policyName!);
        return result.Succeeded;
    }

    public async Task<Result<AppUserDto>> AuthenticateAsync(string email, string password)
    {
        var dbContext = _unitOfWork.GetDbContext();

        var user = await dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
            return Error.NotFound("User_Not_Found", $"User with email {email} not found");

        if (!user.EmailConfirmed)
            return Error.Conflict("Email_Not_Confirmed", $"Email '{email}' not confirmed");

        if (!user.IsActive)
            return Error.Conflict("User_Inactive", "This user account is inactive.");

        if (!await _userManager.CheckPasswordAsync(user, password))
            return Error.Conflict("Invalid_Login_Attempt", "Email / Password are incorrect");

        var roles = await _userManager.GetRolesAsync(user);

        return new AppUserDto(
            user.Id,
            user.Email!,
            roles,
            Permissions: await GetUserPermissionsAsync(user.Id, CancellationToken.None));
    }

    public async Task<string?> GetUserNameAsync(long userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user?.UserName;
    }

    /// <summary>
    /// Retrieves all permissions for a user from both direct user permissions and role-based permissions.
    /// Excludes any explicitly revoked permissions (IsGranted = false).
    /// Only includes permissions that have been explicitly granted (IsGranted = true) or granted through roles.
    /// Returns an empty list if the user has no granted permissions.
    /// </summary>
    public async Task<List<string>> GetUserPermissionsAsync(long userId, CancellationToken ct)
    {
        try
        {
            var dbContext = _unitOfWork.GetDbContext();

            // Get all roles assigned to the user
            var userRoleIds = await dbContext.Set<IdentityUserRole<long>>()
                .AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync(ct);

            // Get all permissions where IsGranted = false for this user (denials)
            var deniedPermissionIds = await dbContext.Set<UserPermission>()
                .AsNoTracking()
                .Where(up => up.UserId == userId && !up.IsGranted)
                .Select(up => up.PermissionId)
                .ToListAsync(ct);

            var allPermissions = new List<string>();

            // Get direct user permissions (granted only)
            var directUserPermissions = await dbContext.Set<UserPermission>()
                .AsNoTracking()
                .Where(up => up.UserId == userId && up.IsGranted)
                .Select(up => up.Permission.Name)
                .ToListAsync(ct);

            allPermissions.AddRange(directUserPermissions);

            // Get role-based permissions, excluding denied permissions
            if (userRoleIds.Any())
            {
                var rolePermissions = await dbContext.Set<RolePermission>()
                    .AsNoTracking()
                    .Where(rp => userRoleIds.Contains(rp.RoleId) && !deniedPermissionIds.Contains(rp.PermissionId))
                    .Select(rp => rp.Permission.Name)
                    .Distinct()
                    .ToListAsync(ct);

                allPermissions.AddRange(rolePermissions);
            }

            var finalPermissions = allPermissions.Distinct().ToList();

            _logger.LogInformation("Retrieved {PermissionCount} permissions for user {UserId} ({DirectCount} direct, {DeniedCount} denied)",
                finalPermissions.Count, userId, directUserPermissions.Count, deniedPermissionIds.Count);

            return finalPermissions;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve permissions for user {UserId}. Returning empty permissions list", userId);
            return [];
        }
    }
}
