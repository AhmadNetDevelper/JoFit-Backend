using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Courses.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Courses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Courses.Queries.GetCoursesPaged;

public class GetCoursesPagedQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCoursesPagedQuery, Result<PagedResult<CourseDto>>>
{
    public async Task<Result<PagedResult<CourseDto>>> Handle(GetCoursesPagedQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<Course>()
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

        var items = entities.ConvertAll(CourseDto.From);

        return new PagedResult<CourseDto>(items, request.Page, request.PageSize, totalCount);
    }
}
