using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Permissions.Dtos;
using JoFit.Domain.Common.Results;

namespace JoFit.Application.Features.Permissions.Queries.GetPermissions;

public sealed record GetPermissionsQuery : ICachedQuery<Result<List<PermissionDto>>>
{
    public string CacheKey => PermissionCacheTags.Permissions;

    public string[] Tags => [PermissionCacheTags.Permissions];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
