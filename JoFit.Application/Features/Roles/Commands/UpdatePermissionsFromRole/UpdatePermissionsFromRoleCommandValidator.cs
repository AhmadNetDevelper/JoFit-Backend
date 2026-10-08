using FluentValidation;

namespace JoFit.Application.Features.Roles.Commands.UpdatePermissionsFromRole;

public sealed class UpdatePermissionsFromRoleCommandValidator : AbstractValidator<UpdatePermissionsFromRoleCommand>
{
    public UpdatePermissionsFromRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).GreaterThan(0);
        RuleFor(x => x.PermissionIds).NotNull();
        RuleForEach(x => x.PermissionIds).GreaterThan(0);
    }
}
