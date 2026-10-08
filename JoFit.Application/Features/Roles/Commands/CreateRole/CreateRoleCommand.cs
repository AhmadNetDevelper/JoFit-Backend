using JoFit.Application.Features.Roles.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Roles.Commands.CreateRole;

public sealed record CreateRoleCommand(
    string Name,
    long? CloneFromRoleId = null) : IRequest<Result<RoleDto>>;
