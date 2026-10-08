using JoFit.Application.Features.Users.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Users.Queries.GetDirectUserPermissions;

/// <summary>
/// Permissions that are not part of the user's roles, with IsGranted = true where granted directly to the user.
/// </summary>
public sealed record GetDirectUserPermissionsQuery(long UserId) : IRequest<Result<List<UserPermissionDto>>>;
