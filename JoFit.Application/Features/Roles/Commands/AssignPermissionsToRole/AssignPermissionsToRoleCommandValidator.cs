using FluentValidation;

namespace JoFit.Application.Features.Roles.Commands.AssignPermissionsToRole;

public sealed class AssignPermissionsToRoleCommandValidator : AbstractValidator<AssignPermissionsToRoleCommand>
{
    public AssignPermissionsToRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).GreaterThan(0);
        RuleFor(x => x.PermissionIds).NotEmpty();
        RuleForEach(x => x.PermissionIds).GreaterThan(0);
    }
}
