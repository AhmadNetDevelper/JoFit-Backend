using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Users.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity;
using JoFit.Domain.Identity.Permissions;
using JoFit.Domain.Identity.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Users.Commands.UpdatePermissionsForUser;

/// <summary>
/// A UserPermission row is only an override of the user's role permissions:
/// - permission in role + granted     => no row needed
/// - permission in role + revoked     => row with IsGranted = false
/// - permission not in role + granted => row with IsGranted = true
/// - permission not in role + revoked => no row needed
/// </summary>
public class UpdatePermissionsForUserCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdatePermissionsForUserCommand, Result<List<UserPermissionDto>>>
{
    public async Task<Result<List<UserPermissionDto>>> Handle(UpdatePermissionsForUserCommand request, CancellationToken ct)
    {
        var dbContext = unitOfWork.GetDbContext();

        var userExists = await dbContext.Set<User>().AsNoTracking().AnyAsync(u => u.Id == request.UserId, ct);
        if (!userExists)
        {
            return UserErrors.UserNotFound;
        }

        // Last value wins if the same permission is sent twice.
        var requested = request.Permissions
            .GroupBy(p => p.PermissionId)
            .ToDictionary(g => g.Key, g => g.Last().IsGranted);

        var requestedIds = requested.Keys.ToList();

        var existingPermissionIds = await unitOfWork.GetRepository<Permission>().GetTableNoTracking()
            .Where(p => requestedIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(ct);

        var missingIds = requestedIds.Except(existingPermissionIds).ToList();
        if (missingIds.Count > 0)
        {
            return Error.NotFound(
                code: "Permission.NotFound",
                description: $"Some permissions were not found: {string.Join(", ", missingIds)}");
        }

        var userRoleIds = await dbContext.Set<IdentityUserRole<long>>().AsNoTracking()
            .Where(ur => ur.UserId == request.UserId)
            .Select(ur => ur.RoleId)
            .ToListAsync(ct);

        var rolePermissionIds = (await dbContext.Set<RolePermission>().AsNoTracking()
            .Where(rp => userRoleIds.Contains(rp.RoleId))
            .Select(rp => rp.PermissionId)
            .Distinct()
            .ToListAsync(ct)).ToHashSet();

        var userPermissionRepository = unitOfWork.GetRepository<UserPermission>();

        var existing = await userPermissionRepository.GetTableAsTracking()
            .Where(up => up.UserId == request.UserId && requestedIds.Contains(up.PermissionId))
            .ToDictionaryAsync(up => up.PermissionId, ct);

        var toAdd = new List<UserPermission>();
        var toDelete = new List<UserPermission>();

        foreach (var (permissionId, isGranted) in requested)
        {
            var inRole = rolePermissionIds.Contains(permissionId);
            existing.TryGetValue(permissionId, out var current);

            // Row is only needed when the request differs from what the role already gives.
            var rowNeeded = inRole != isGranted;

            if (!rowNeeded)
            {
                if (current is not null)
                {
                    toDelete.Add(current);
                }
            }
            else if (current is null)
            {
                toAdd.Add(new UserPermission
                {
                    UserId = request.UserId,
                    PermissionId = permissionId,
                    IsGranted = isGranted
                });
            }
            else
            {
                current.IsGranted = isGranted;
            }
        }

        if (toDelete.Count > 0)
        {
            userPermissionRepository.DeleteRangeAsync(toDelete, softDelete: false);
        }

        if (toAdd.Count > 0)
        {
            await userPermissionRepository.AddRangeAsync(toAdd, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);

        return await dbContext.Set<UserPermission>().AsNoTracking()
            .Where(up => up.UserId == request.UserId)
            .Select(up => new UserPermissionDto(
                up.UserId,
                up.PermissionId,
                up.Permission!.Name,
                up.Permission.Resource,
                up.Permission.Action,
                up.IsGranted))
            .ToListAsync(ct);
    }
}
