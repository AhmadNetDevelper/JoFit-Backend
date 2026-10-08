using FluentValidation;

namespace JoFit.Application.Features.Roles.Commands.RemoveRole;

public sealed class RemoveRoleCommandValidator : AbstractValidator<RemoveRoleCommand>
{
    public RemoveRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).GreaterThan(0);
    }
}
