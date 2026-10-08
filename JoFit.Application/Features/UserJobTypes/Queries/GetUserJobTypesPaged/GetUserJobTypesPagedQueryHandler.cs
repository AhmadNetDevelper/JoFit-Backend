using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.UserJobTypes.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.UserJobTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.UserJobTypes.Queries.GetUserJobTypesPaged;

public class GetUserJobTypesPagedQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetUserJobTypesPagedQuery, Result<PagedResult<UserJobTypeDto>>>
{
    public async Task<Result<PagedResult<UserJobTypeDto>>> Handle(GetUserJobTypesPagedQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<UserJobType>()
            .GetTableNoTracking()
            .Where(x => !x.IsDeleted);

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.NameAr.Contains(search) || x.NameEn.Contains(search));
        }

        var totalCount = await query.CountAsync(ct);

        var entities = await query
            .OrderBy(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var items = entities.ConvertAll(UserJobTypeDto.From);

        return new PagedResult<UserJobTypeDto>(items, request.Page, request.PageSize, totalCount);
    }
}
