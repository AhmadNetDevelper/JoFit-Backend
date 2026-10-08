using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Courses.Dtos;
using JoFit.Domain.Common.Results;

namespace JoFit.Application.Features.Courses.Queries.GetCoursesPaged;

public sealed record GetCoursesPagedQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    bool? IsActive = null) : ICachedQuery<Result<PagedResult<CourseDto>>>
{
    public string CacheKey =>
        $"{CourseCacheTags.Courses}:paged:{Page}:{PageSize}:{Search?.Trim().ToLowerInvariant()}:{IsActive}";

    public string[] Tags => [CourseCacheTags.Courses];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
