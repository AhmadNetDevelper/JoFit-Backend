using JoFit.Domain.Courses;

namespace JoFit.Application.Features.Courses.Dtos;

public sealed record CourseDto(
    long Id,
    string NameAr,
    string NameEn,
    bool IsActive)
{
    public static CourseDto From(Course entity)
        => new(entity.Id, entity.NameAr, entity.NameEn, entity.IsActive);
}
