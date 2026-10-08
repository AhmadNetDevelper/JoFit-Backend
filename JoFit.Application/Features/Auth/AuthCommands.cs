using FluentValidation;
using JoFit.Application.Common.Interfaces;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity.Users;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace JoFit.Application.Features.Auth;

public sealed record SignInCommand(string Email, string Password) : IRequest<Result<SignedInDto>>;

public sealed record RegisterCommand(string FullName, string Email, string? Phone, string Password, string Locale, bool MarketingOptIn,
    IFormFile? ProfileImage = null, DateTime? BirthOfDate = null, EnumGender GenderId = default, long? CountryId = null, long? CityId = null)
    : IRequest<Result<SignedInDto>>;

public sealed record SignOutCommand(long? SessionId, string? RefreshToken) : IRequest<Result<Deleted>>;

public sealed record RefreshCommand(string RefreshToken) : IRequest<Result<SignedInDto>>;

public sealed record GetSessionQuery(long UserId) : IRequest<Result<SessionDto>>;

public sealed record ChangePasswordCommand(long UserId, long? SessionId, string CurrentPassword, string NewPassword)
    : IRequest<Result<Deleted>>;

public sealed record ChangeEmailCommand(long UserId, string? AccountRole, string Email, string CurrentPassword)
    : IRequest<Result<SessionDto>>;

public sealed record RevokeOtherSessionsCommand(long UserId, long? SessionId) : IRequest<Result<Deleted>>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FullName).Must(n => AccountRules.CollapseWhitespace(n).Length >= 2).WithMessage("Enter your full name.");
        RuleFor(x => x.Email).Email();
        RuleFor(x => x.Phone).OptionalPhone();
        RuleFor(x => x.Password).Password();
    }
}

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.NewPassword).Password();
        RuleFor(x => x.NewPassword)
            .Must((command, next) => next != command.CurrentPassword)
            .WithMessage("Choose a password different from the current one.");
    }
}

public sealed class ChangeEmailCommandValidator : AbstractValidator<ChangeEmailCommand>
{
    public ChangeEmailCommandValidator()
    {
        RuleFor(x => x.Email).Email();
    }
}

public class SignInCommandHandler(IAccountService accounts) : IRequestHandler<SignInCommand, Result<SignedInDto>>
{
    public Task<Result<SignedInDto>> Handle(SignInCommand request, CancellationToken ct)
        => accounts.SignInAsync(request.Email ?? string.Empty, request.Password ?? string.Empty, ct);
}

public class RegisterCommandHandler(IAccountService accounts) : IRequestHandler<RegisterCommand, Result<SignedInDto>>
{
    public Task<Result<SignedInDto>> Handle(RegisterCommand request, CancellationToken ct)
        => accounts.RegisterCustomerAsync(
            new RegisterCustomerInput(
                AccountRules.CollapseWhitespace(request.FullName),
                request.Email.Trim(),
                string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                request.Password,
                request.Locale,
                request.MarketingOptIn,
                request.ProfileImage,
                request.BirthOfDate,
                request.GenderId,
                request.CountryId,
                request.CityId),
            ct);
}

public class SignOutCommandHandler(IAccountService accounts) : IRequestHandler<SignOutCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(SignOutCommand request, CancellationToken ct)
    {
        await accounts.RevokeSessionAsync(request.SessionId, request.RefreshToken, ct);
        return Result.Deleted;
    }
}

public class RefreshCommandHandler(IAccountService accounts) : IRequestHandler<RefreshCommand, Result<SignedInDto>>
{
    public Task<Result<SignedInDto>> Handle(RefreshCommand request, CancellationToken ct)
        => accounts.RefreshAsync(request.RefreshToken ?? string.Empty, ct);
}

public class GetSessionQueryHandler(IAccountService accounts) : IRequestHandler<GetSessionQuery, Result<SessionDto>>
{
    public Task<Result<SessionDto>> Handle(GetSessionQuery request, CancellationToken ct)
        => accounts.GetSessionAsync(request.UserId, ct);
}

public class ChangePasswordCommandHandler(IAccountService accounts) : IRequestHandler<ChangePasswordCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var result = await accounts.ChangePasswordAsync(request.UserId, request.SessionId, request.CurrentPassword, request.NewPassword, ct);
        return result.IsError ? result.Errors : Result.Deleted;
    }
}

public class ChangeEmailCommandHandler(IAccountService accounts) : IRequestHandler<ChangeEmailCommand, Result<SessionDto>>
{
    public Task<Result<SessionDto>> Handle(ChangeEmailCommand request, CancellationToken ct)
    {
        // The login email of a coach or an employer is part of the profile staff verify.
        if (request.AccountRole != "customer")
        {
            return Task.FromResult<Result<SessionDto>>(
                Error.Forbidden("Auth.EmailChangeNotAllowed", "Contact JoFit to change the email of this account."));
        }

        return accounts.ChangeEmailAsync(request.UserId, request.Email, request.CurrentPassword, ct);
    }
}

public class RevokeOtherSessionsCommandHandler(IAccountService accounts) : IRequestHandler<RevokeOtherSessionsCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(RevokeOtherSessionsCommand request, CancellationToken ct)
    {
        await accounts.RevokeOtherSessionsAsync(request.UserId, request.SessionId, ct);
        return Result.Deleted;
    }
}
