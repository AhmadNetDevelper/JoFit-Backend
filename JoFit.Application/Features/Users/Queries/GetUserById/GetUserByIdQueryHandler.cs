using JoFit.Application.Features.Users.Dtos;
using JoFit.Application.Features.Users.Mappers;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity.Roles;
using JoFit.Domain.Identity.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler(UserManager<User> userManager, RoleManager<Role> roleManager)
    : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        var user = await userManager.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null)
        {
            return UserErrors.UserNotFound;
        }

        var roleName = (await userManager.GetRolesAsync(user)).FirstOrDefault();
        var role = roleName is null ? null : await roleManager.FindByNameAsync(roleName);

        return user.ToDto(role?.Id, roleName);
    }
}
