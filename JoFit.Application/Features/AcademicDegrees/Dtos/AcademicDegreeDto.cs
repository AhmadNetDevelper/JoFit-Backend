using JoFit.Domain.AcademicDegrees;

namespace JoFit.Application.Features.AcademicDegrees.Dtos;

public sealed record AcademicDegreeDto(
    long Id,
    string NameAr,
    string NameEn,
    bool IsActive)
{
    public static AcademicDegreeDto From(AcademicDegree entity)
        => new(entity.Id, entity.NameAr, entity.NameEn, entity.IsActive);
}
