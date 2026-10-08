using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Courses.Dtos;
using JoFit.Domain.Common.Results;

namespace JoFit.Application.Features.Courses.Queries.GetAllCourses;

/// <param name="Language">Normalized language code ("ar" or "en"). Part of the cache key so languages never mix.</param>
public sealed record GetAllCoursesQuery(string Language)
    : ICachedQuery<Result<List<CourseLookupDto>>>
{
    public string CacheKey => $"{CourseCacheTags.Courses}:all:{Language}";

    public string[] Tags => [CourseCacheTags.Courses];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
