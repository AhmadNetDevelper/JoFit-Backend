using JoFit.Domain.Identity;
using JoFit.Domain.Identity.Permissions;
using JoFit.Domain.Identity.Roles;
using JoFit.Domain.Identity.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Infrastructure.Data.Seeders;

public class IdentitySeeder
{
    private readonly JoFitDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;

    public IdentitySeeder(JoFitDbContext context, UserManager<User> userManager, RoleManager<Role> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        await SeedPermissionsAsync();
        await SeedRolesAsync();
        await SeedSuperAdminUserAsync();
        await SeedSuperAdminRolePermissionsAsync();
        await SeedStaffRolesAsync();
        await RetireStaffRolesAsync();
    }

    /// <summary>Moves the members of a role that no longer exists to its replacement, then removes the old role.</summary>
    private async Task RetireStaffRolesAsync()
    {
        foreach (var (oldName, newName) in Role.Retired)
        {
            var oldRole = await _roleManager.FindByNameAsync(oldName);
            if (oldRole is null) continue;

            foreach (var user in await _userManager.GetUsersInRoleAsync(oldName))
            {
                if (!await _userManager.IsInRoleAsync(user, newName))
                    await _userManager.AddToRoleAsync(user, newName);
                await _userManager.RemoveFromRoleAsync(user, oldName);
                Console.WriteLine($"  ✅ {user.Email} moved from '{oldName}' to '{newName}'");
            }

            var oldPermissions = await _context.RolePermissions.Where(rp => rp.RoleId == oldRole.Id).ToListAsync();
            _context.RolePermissions.RemoveRange(oldPermissions);
            await _context.SaveChangesAsync();

            await _roleManager.DeleteAsync(oldRole);
            Console.WriteLine($"  ✅ Role removed (retired): {oldName}");
        }
    }

    private async Task SeedPermissionsAsync()
    {
        var permissions = SeedData.Permissions.GetAll().ToList();
        var currentPermissionNames = permissions.Select(p => p.Name).ToHashSet();
        var addedCount = 0;
        var skippedCount = 0;

        foreach (var permissionData in permissions)
        {
            try
            {
                var existingPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Name == permissionData.Name);

                if (existingPermission == null)
                {
                    var createPermissionResult = Permission.Create(
                        permissionData.Name,
                        permissionData.Resource,
                        permissionData.Action);

                    if (createPermissionResult.IsError)
                        throw new InvalidOperationException($"Failed to create permission '{permissionData.Name}': {createPermissionResult.Errors}");

                    _context.Permissions.Add(createPermissionResult.Value!);
                    addedCount++;
                    Console.WriteLine($"  ✅ Permission added: {permissionData.Name}");
                }
                else
                {
                    skippedCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Error processing permission '{permissionData.Name}': {ex.Message}");
                throw;
            }
        }

        if (addedCount > 0)
            await _context.SaveChangesAsync();

        // Remove permissions that no longer exist in the Permissions class.
        // RolePermission/UserPermission rows for them cascade-delete with the Permission.
        var stalePermissions = await _context.Permissions
            .Where(p => !currentPermissionNames.Contains(p.Name))
            .ToListAsync();

        var removedCount = stalePermissions.Count;
        if (removedCount > 0)
        {
            foreach (var stale in stalePermissions)
                Console.WriteLine($"  ✅ Permission removed (no longer defined): {stale.Name}");

            _context.Permissions.RemoveRange(stalePermissions);
            await _context.SaveChangesAsync();
        }

        if (addedCount > 0 || removedCount > 0)
        {
            Console.WriteLine($"✅ Permissions: {addedCount} added, {removedCount} removed, {skippedCount} skipped.");
        }
        else
        {
            Console.WriteLine($"⏭ Permissions: {skippedCount} already exist.");
        }
    }

    /// <summary>Creates the back-office roles and keeps their permissions in line with Role.</summary>
    private async Task SeedStaffRolesAsync()
    {
        var permissionsByName = await _context.Permissions.ToDictionaryAsync(p => p.Name);
        foreach (var roleName in Role.StaffRoles)
        {
            await EnsureRoleAsync(roleName);
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null) continue;

            var wanted = Role.PermissionsFor(roleName)
                .Where(permissionsByName.ContainsKey)
                .Select(name => permissionsByName[name].Id)
                .ToHashSet();
            var current = await _context.RolePermissions.Where(rp => rp.RoleId == role.Id).ToListAsync();

            _context.RolePermissions.RemoveRange(current.Where(rp => !wanted.Contains(rp.PermissionId)));
            foreach (var permissionId in wanted.Where(id => current.All(rp => rp.PermissionId != id)))
                _context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedRolesAsync()
    {
        foreach (var roleName in SeedData.Roles.All)
            await EnsureRoleAsync(roleName);
    }

    private async Task EnsureRoleAsync(string roleName)
    {
        var existing = await _roleManager.FindByNameAsync(roleName);
        if (existing == null)
        {
            var role = new Role { Name = roleName, IsActive = true };
            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");

            Console.WriteLine($"  ✅ Role seeded: {roleName}");
        }
        else
        {
            Console.WriteLine($"  ⏭ Role already exists: {roleName}");
        }
    }

    private async Task SeedSuperAdminUserAsync()
    {
        var existingSuperAdmin = await _userManager.FindByEmailAsync(SeedData.SuperAdmin.Email);

        if (existingSuperAdmin == null)
        {
            var superAdminUser = new User
            {
                UserName = SeedData.SuperAdmin.UserName,
                Email = SeedData.SuperAdmin.Email,
                EmailConfirmed = true,
                PhoneNumber = SeedData.SuperAdmin.PhoneNumber,
                PhoneNumberConfirmed = true,
                GenderId = EnumGender.Male,
                IsActive = true
            };

            var createUserResult = await _userManager.CreateAsync(superAdminUser, SeedData.SuperAdmin.Password);
            if (!createUserResult.Succeeded)
                throw new InvalidOperationException($"Failed to create SuperAdmin user: {string.Join(", ", createUserResult.Errors.Select(e => e.Description))}");

            var assignRoleResult = await _userManager.AddToRoleAsync(superAdminUser, SeedData.Roles.SuperAdmin);
            if (!assignRoleResult.Succeeded)
                throw new InvalidOperationException($"Failed to assign SuperAdmin role: {string.Join(", ", assignRoleResult.Errors.Select(e => e.Description))}");

            Console.WriteLine("  ✅ SuperAdmin user seeded.");
        }
        else
        {
            Console.WriteLine("  ⏭ SuperAdmin user already exists.");
        }
    }

    private async Task SeedSuperAdminRolePermissionsAsync()
    {
        var superAdminRole = await _roleManager.FindByNameAsync(SeedData.Roles.SuperAdmin);
        if (superAdminRole == null) return;

        var allPermissions = await _context.Permissions.ToListAsync();
        var existingRolePermissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == superAdminRole.Id)
            .ToListAsync();

        var addedCount = 0;

        foreach (var permission in allPermissions)
        {
            var permissionExists = existingRolePermissions.Any(rp => rp.PermissionId == permission.Id);
            if (!permissionExists)
            {
                _context.RolePermissions.Add(new RolePermission { RoleId = superAdminRole.Id, PermissionId = permission.Id });
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ SuperAdmin role permissions: {addedCount} assigned.");
        }
        else
        {
            Console.WriteLine("⏭ SuperAdmin role permissions: all already assigned.");
        }
    }
}
