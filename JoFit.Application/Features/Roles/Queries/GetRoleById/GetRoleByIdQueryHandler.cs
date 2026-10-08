using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Roles.Dtos;
using JoFit.Application.Features.Roles.Mappers;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity;
using JoFit.Domain.Identity.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Roles.Queries.GetRoleById;

public class GetRoleByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetRoleByIdQuery, Result<RolePermissionDto>>
{
    public async Task<Result<RolePermissionDto>> Handle(GetRoleByIdQuery request, CancellationToken ct)
    {
        var role = await unitOfWork.GetRepository<Role>().GetByIdAsync(request.RoleId, ct);
        if (role is null)
        {
            return RoleErrors.RoleNotFound;
        }

        var permissions = await unitOfWork.GetRepository<RolePermission>().GetTableNoTracking()
            .Where(rp => rp.RoleId == request.RoleId)
            .Select(rp => rp.Permission)
            .ToListAsync(ct);

        return role.ToRolePermissionDto(permissions);
    }
}
