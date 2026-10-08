using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Roles.Dtos;
using JoFit.Application.Features.Roles.Mappers;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity;
using JoFit.Domain.Identity.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.Roles.Commands.CreateRole;

public class CreateRoleCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<CreateRoleCommand, Result<RoleDto>>
{
    public async Task<Result<RoleDto>> Handle(CreateRoleCommand request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        var normalizedName = name.ToUpperInvariant();
        var roleRepository = unitOfWork.GetRepository<Role>();

        var exists = await roleRepository.GetTableNoTracking()
            .AnyAsync(r => r.NormalizedName == normalizedName, ct);
        if (exists)
        {
            return RoleErrors.RoleExists;
        }

        if (request.CloneFromRoleId.HasValue)
        {
            var sourceExists = await roleRepository.GetTableNoTracking()
                .AnyAsync(r => r.Id == request.CloneFromRoleId.Value, ct);
            if (!sourceExists)
            {
                return RoleErrors.RoleNotFound;
            }
        }

        var role = new Role { Name = name, NormalizedName = normalizedName, IsActive = true };

        await roleRepository.AddAsync(role, ct);
        await unitOfWork.SaveChangesAsync(ct);

        if (request.CloneFromRoleId.HasValue)
        {
            var rolePermissionRepository = unitOfWork.GetRepository<RolePermission>();

            var sourcePermissionIds = await rolePermissionRepository.GetTableNoTracking()
                .Where(rp => rp.RoleId == request.CloneFromRoleId.Value)
                .Select(rp => rp.PermissionId)
                .ToListAsync(ct);

            if (sourcePermissionIds.Count > 0)
            {
                var cloned = sourcePermissionIds
                    .Select(permissionId => new RolePermission { RoleId = role.Id, PermissionId = permissionId })
                    .ToList();

                await rolePermissionRepository.AddRangeAsync(cloned, ct);
                await unitOfWork.SaveChangesAsync(ct);
            }
        }

        await cache.RemoveByTagAsync(RoleCacheTags.Roles, ct);

        return role.ToDto();
    }
}
