using JoFit.Application.Features.Identity.Dtos;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Identity.Queries.GetUserInfo;

public sealed record GetUserByIdQuery(long UserId) : IRequest<Result<AppUserDto>>;
