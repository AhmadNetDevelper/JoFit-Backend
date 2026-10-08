using FluentValidation;

namespace JoFit.Application.Features.Users.Commands.UpdatePermissionsForUser;

public sealed class UpdatePermissionsForUserCommandValidator : AbstractValidator<UpdatePermissionsForUserCommand>
{
    public UpdatePermissionsForUserCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Permissions).NotEmpty();
        RuleForEach(x => x.Permissions)
            .ChildRules(permission => permission.RuleFor(p => p.PermissionId).GreaterThan(0));
    }
}
