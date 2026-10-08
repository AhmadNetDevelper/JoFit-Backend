using JoFit.Application.Features.Identity.Dtos;
using JoFit.Contracts.Requests.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string? policyName);

    Task<Result<AppUserDto>> AuthenticateAsync(string email, string password);

    Task<Result<AppUserDto>> GetUserByIdAsync(long userId, CancellationToken ct);

    Task<Result<AppUserDto>> RegisterAsync(RegisterRequest request, CancellationToken ct);

    Task<string?> GetUserNameAsync(long userId);

    Task<List<string>> GetUserPermissionsAsync(long userId, CancellationToken ct);
}
