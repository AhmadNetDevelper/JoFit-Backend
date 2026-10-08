using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Roles.Dtos;
using JoFit.Application.Features.Roles.Mappers;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.Roles.Commands.UpdateRole;

public class UpdateRoleCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<UpdateRoleCommand, Result<RoleDto>>
{
    public async Task<Result<RoleDto>> Handle(UpdateRoleCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<Role>();

        var role = await repository.GetTableAsTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, ct);
        if (role is null)
        {
            return RoleErrors.RoleNotFound;
        }

        var name = request.Name.Trim();
        var normalizedName = name.ToUpperInvariant();

        var duplicate = await repository.GetTableNoTracking()
            .AnyAsync(r => r.Id != request.RoleId && r.NormalizedName == normalizedName, ct);
        if (duplicate)
        {
            return RoleErrors.RoleExists;
        }

        role.Name = name;
        role.NormalizedName = normalizedName;

        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(RoleCacheTags.Roles, ct);

        return role.ToDto();
    }
}
