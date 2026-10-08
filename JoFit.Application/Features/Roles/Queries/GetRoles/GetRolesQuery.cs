using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Roles.Dtos;
using JoFit.Domain.Common.Results;

namespace JoFit.Application.Features.Roles.Queries.GetRoles;

public sealed record GetRolesQuery : ICachedQuery<Result<List<RoleDto>>>
{
    public string CacheKey => RoleCacheTags.Roles;

    public string[] Tags => [RoleCacheTags.Roles];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
