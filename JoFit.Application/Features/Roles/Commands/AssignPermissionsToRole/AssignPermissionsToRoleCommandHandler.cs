using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Roles.Dtos;
using JoFit.Application.Features.Roles.Mappers;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity;
using JoFit.Domain.Identity.Permissions;
using JoFit.Domain.Identity.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.Roles.Commands.AssignPermissionsToRole;

public class AssignPermissionsToRoleCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<AssignPermissionsToRoleCommand, Result<RolePermissionDto>>
{
    public async Task<Result<RolePermissionDto>> Handle(AssignPermissionsToRoleCommand request, CancellationToken ct)
    {
        var role = await unitOfWork.GetRepository<Role>().GetByIdAsync(request.RoleId, ct);
        if (role is null)
        {
            return RoleErrors.RoleNotFound;
        }

        var requestedIds = request.PermissionIds.Distinct().ToList();

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

        var rolePermissionRepository = unitOfWork.GetRepository<RolePermission>();

        var assignedIds = await rolePermissionRepository.GetTableNoTracking()
            .Where(rp => rp.RoleId == request.RoleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync(ct);

        var toAdd = requestedIds
            .Except(assignedIds)
            .Select(permissionId => new RolePermission { RoleId = request.RoleId, PermissionId = permissionId })
            .ToList();

        if (toAdd.Count > 0)
        {
            await rolePermissionRepository.AddRangeAsync(toAdd, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await cache.RemoveByTagAsync(RoleCacheTags.Roles, ct);
        }

        var permissions = await rolePermissionRepository.GetTableNoTracking()
            .Where(rp => rp.RoleId == request.RoleId)
            .Select(rp => rp.Permission)
            .ToListAsync(ct);

        return role.ToRolePermissionDto(permissions);
    }
}
