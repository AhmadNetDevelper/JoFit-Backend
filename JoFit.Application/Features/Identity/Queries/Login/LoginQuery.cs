using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Identity.Queries.Login
{
    public record LoginQuery(string Email, string Password) : IRequest<Result<TokenResponse>>;
}
