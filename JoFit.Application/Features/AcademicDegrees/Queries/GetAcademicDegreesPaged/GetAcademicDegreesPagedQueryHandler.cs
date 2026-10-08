using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.AcademicDegrees.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.AcademicDegrees;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.AcademicDegrees.Queries.GetAcademicDegreesPaged;

public class GetAcademicDegreesPagedQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAcademicDegreesPagedQuery, Result<PagedResult<AcademicDegreeDto>>>
{
    public async Task<Result<PagedResult<AcademicDegreeDto>>> Handle(GetAcademicDegreesPagedQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<AcademicDegree>()
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

        var items = entities.ConvertAll(AcademicDegreeDto.From);

        return new PagedResult<AcademicDegreeDto>(items, request.Page, request.PageSize, totalCount);
    }
}
