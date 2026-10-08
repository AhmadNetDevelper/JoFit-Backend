using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.JobStatuses.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.JobStatuses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.JobStatuses.Queries.GetJobStatusesPaged;

public class GetJobStatusesPagedQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetJobStatusesPagedQuery, Result<PagedResult<JobStatusDto>>>
{
    public async Task<Result<PagedResult<JobStatusDto>>> Handle(GetJobStatusesPagedQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<JobStatus>()
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

        var items = entities.ConvertAll(JobStatusDto.From);

        return new PagedResult<JobStatusDto>(items, request.Page, request.PageSize, totalCount);
    }
}
