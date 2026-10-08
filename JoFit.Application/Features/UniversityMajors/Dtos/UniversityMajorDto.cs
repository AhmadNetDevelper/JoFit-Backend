using JoFit.Domain.UniversityMajors;

namespace JoFit.Application.Features.UniversityMajors.Dtos;

public sealed record UniversityMajorDto(
    long Id,
    string NameAr,
    string NameEn,
    bool IsActive)
{
    public static UniversityMajorDto From(UniversityMajor entity)
        => new(entity.Id, entity.NameAr, entity.NameEn, entity.IsActive);
}
