using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace JoFit.Application.Features.Users.Commands.RemoveUser;

public class RemoveUserCommandHandler(UserManager<User> userManager)
    : IRequestHandler<RemoveUserCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(RemoveUserCommand request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return UserErrors.UserNotFound;
        }

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return Error.Failure("User.DeleteFailed", "Failed to delete the user.");
        }

        return Result.Deleted;
    }
}
