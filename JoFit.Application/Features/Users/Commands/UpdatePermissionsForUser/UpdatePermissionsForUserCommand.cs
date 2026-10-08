using JoFit.Application.Features.Users.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Users.Commands.UpdatePermissionsForUser;

public sealed record UserPermissionCommandRequest(long PermissionId, bool IsGranted);

/// <summary>
/// Adds, updates or removes the user's direct permission overrides based on the requested grant status.
/// </summary>
public sealed record UpdatePermissionsForUserCommand(
    long UserId,
    List<UserPermissionCommandRequest> Permissions) : IRequest<Result<List<UserPermissionDto>>>;
