using FluentValidation;

namespace JoFit.Application.Features.Roles.Commands.CreateRole;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.CloneFromRoleId).GreaterThan(0).When(x => x.CloneFromRoleId.HasValue);
    }
}
