using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.JobTypes.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.JobTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.JobTypes.Queries.GetJobTypesPaged;

public class GetJobTypesPagedQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetJobTypesPagedQuery, Result<PagedResult<JobTypeDto>>>
{
    public async Task<Result<PagedResult<JobTypeDto>>> Handle(GetJobTypesPagedQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<JobType>()
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

        var items = entities.ConvertAll(JobTypeDto.From);

        return new PagedResult<JobTypeDto>(items, request.Page, request.PageSize, totalCount);
    }
}
