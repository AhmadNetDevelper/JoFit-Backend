using JoFit.Application.Features.Roles.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Roles.Queries.GetRoleById;

public sealed record GetRoleByIdQuery(long RoleId) : IRequest<Result<RolePermissionDto>>;
