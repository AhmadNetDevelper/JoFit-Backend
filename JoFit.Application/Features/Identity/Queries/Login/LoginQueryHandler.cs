using JoFit.Application.Common.Interfaces;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace JoFit.Application.Features.Identity.Queries.Login;

public class LoginQueryHandler(ILogger<LoginQueryHandler> logger,
                               IIdentityService identityService,
                               ITokenProvider tokenProvider) : IRequestHandler<LoginQuery, Result<TokenResponse>>
{
    private readonly ILogger<LoginQueryHandler> _logger = logger;
    private readonly IIdentityService _identityService = identityService;
    private readonly ITokenProvider _tokenProvider = tokenProvider;

    public async Task<Result<TokenResponse>> Handle(LoginQuery query, CancellationToken ct)
    {
        var userResponse = await _identityService.AuthenticateAsync(query.Email, query.Password);

        if (userResponse.IsError)
        {
            return userResponse.Errors;
        }

        var generateTokenResult = await _tokenProvider.GenerateJwtTokenAsync(userResponse.Value, ct);

        if (generateTokenResult.IsError)
        {
            _logger.LogError("Generate token error occurred: {ErrorDescription}", generateTokenResult.TopError.Description);

            return generateTokenResult.Errors;
        }

        var tokenResponse = generateTokenResult.Value;

        return tokenResponse;
    }
}
