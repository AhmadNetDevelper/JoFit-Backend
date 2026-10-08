using JoFit.Application.Features.Users.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Users.Queries.GetRoleUserPermissions;

/// <summary>
/// Permissions the user gets through their roles, with IsGranted = false where a direct override revokes it.
/// </summary>
public sealed record GetRoleUserPermissionsQuery(long UserId) : IRequest<Result<List<UserPermissionDto>>>;
