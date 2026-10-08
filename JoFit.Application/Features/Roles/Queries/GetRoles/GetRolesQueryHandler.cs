using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Roles.Dtos;
using JoFit.Application.Features.Roles.Mappers;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Roles.Queries.GetRoles;

public class GetRolesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetRolesQuery, Result<List<RoleDto>>>
{
    public async Task<Result<List<RoleDto>>> Handle(GetRolesQuery request, CancellationToken ct)
    {
        var roles = await unitOfWork.GetRepository<Role>()
            .GetTableNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

        return roles.ToDtos();
    }
}
