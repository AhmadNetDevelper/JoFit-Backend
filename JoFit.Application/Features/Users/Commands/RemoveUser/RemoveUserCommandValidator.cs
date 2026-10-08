using FluentValidation;

namespace JoFit.Application.Features.Users.Commands.RemoveUser;

public sealed class RemoveUserCommandValidator : AbstractValidator<RemoveUserCommand>
{
    public RemoveUserCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}
