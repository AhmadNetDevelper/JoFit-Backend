using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Users.Dtos;
using JoFit.Application.Features.Users.Mappers;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity.Roles;
using JoFit.Domain.Identity.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Users.Queries.GetUsersPaged;

public class GetUsersPagedQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetUsersPagedQuery, Result<PagedResult<UserDto>>>
{
    public async Task<Result<PagedResult<UserDto>>> Handle(GetUsersPagedQuery request, CancellationToken ct)
    {
        var dbContext = unitOfWork.GetDbContext();

        var query = dbContext.Set<User>().AsNoTracking().AsQueryable();

        if (request.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(u =>
                u.UserName!.Contains(search)
                || u.Email!.Contains(search)
                || u.PhoneNumber!.Contains(search)
                || u.FullName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.RoleName))
        {
            var normalizedRole = request.RoleName.Trim().ToUpperInvariant();
            query = query.Where(u => dbContext.Set<IdentityUserRole<long>>()
                .Any(ur => ur.UserId == u.Id
                    && dbContext.Set<Role>().Any(r => r.Id == ur.RoleId && r.NormalizedName == normalizedRole)));
        }

        var totalCount = await query.CountAsync(ct);

        var users = await query
            .OrderBy(u => u.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var userIds = users.ConvertAll(u => u.Id);

        var userRoles = await dbContext.Set<IdentityUserRole<long>>().AsNoTracking()
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(dbContext.Set<Role>().AsNoTracking(), ur => ur.RoleId, r => r.Id,
                (ur, r) => new { ur.UserId, RoleId = r.Id, r.Name })
            .ToListAsync(ct);

        var roleByUser = userRoles
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.First());

        var items = users
            .ConvertAll(u => roleByUser.TryGetValue(u.Id, out var role)
                ? u.ToDto(role.RoleId, role.Name)
                : u.ToDto(null, null));

        return new PagedResult<UserDto>(items, request.Page, request.PageSize, totalCount);
    }
}
