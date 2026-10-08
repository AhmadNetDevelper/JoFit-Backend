using JoFit.Application.Common.Models;
using JoFit.Application.Features.Users.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Users.Queries.GetUsersPaged;

public sealed record GetUsersPagedQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null,
    string? RoleName = null) : IRequest<Result<PagedResult<UserDto>>>;
