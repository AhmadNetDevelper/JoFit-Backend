using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Permissions.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity.Permissions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Permissions.Queries.GetPermissions;

public class GetPermissionsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetPermissionsQuery, Result<List<PermissionDto>>>
{
    public async Task<Result<List<PermissionDto>>> Handle(GetPermissionsQuery request, CancellationToken ct)
    {
        return await unitOfWork.GetRepository<Permission>()
            .GetTableNoTracking()
            .OrderBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .Select(p => new PermissionDto(p.Id, p.Name, p.Resource, p.Action))
            .ToListAsync(ct);
    }
}
