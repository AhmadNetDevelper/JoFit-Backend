using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Users.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity;
using JoFit.Domain.Identity.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Users.Queries.GetRoleUserPermissions;

public class GetRoleUserPermissionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetRoleUserPermissionsQuery, Result<List<UserPermissionDto>>>
{
    public async Task<Result<List<UserPermissionDto>>> Handle(GetRoleUserPermissionsQuery request, CancellationToken ct)
    {
        var dbContext = unitOfWork.GetDbContext();

        var userExists = await dbContext.Set<User>().AsNoTracking().AnyAsync(u => u.Id == request.UserId, ct);
        if (!userExists)
        {
            return UserErrors.UserNotFound;
        }

        var userRoleIds = await dbContext.Set<IdentityUserRole<long>>().AsNoTracking()
            .Where(ur => ur.UserId == request.UserId)
            .Select(ur => ur.RoleId)
            .ToListAsync(ct);

        var rolePermissions = await dbContext.Set<RolePermission>().AsNoTracking()
            .Where(rp => userRoleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission)
            .Distinct()
            .ToListAsync(ct);

        var overrides = await dbContext.Set<UserPermission>().AsNoTracking()
            .Where(up => up.UserId == request.UserId)
            .ToDictionaryAsync(up => up.PermissionId, up => up.IsGranted, ct);

        return rolePermissions
            .Select(p => new UserPermissionDto(
                request.UserId,
                p.Id,
                p.Name,
                p.Resource,
                p.Action,
                overrides.TryGetValue(p.Id, out var isGranted) ? isGranted : true))
            .ToList();
    }
}
