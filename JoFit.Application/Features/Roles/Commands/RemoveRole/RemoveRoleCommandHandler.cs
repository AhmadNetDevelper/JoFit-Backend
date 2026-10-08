using JoFit.Application.Common.Interfaces;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.Roles.Commands.RemoveRole;

public class RemoveRoleCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<RemoveRoleCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(RemoveRoleCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<Role>();

        var role = await repository.GetTableAsTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, ct);
        if (role is null)
        {
            return RoleErrors.RoleNotFound;
        }

        // Roles are not soft-deletable; RolePermission rows cascade-delete with the role.
        repository.DeleteAsync(role, softDelete: false);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(RoleCacheTags.Roles, ct);

        return Result.Deleted;
    }
}
