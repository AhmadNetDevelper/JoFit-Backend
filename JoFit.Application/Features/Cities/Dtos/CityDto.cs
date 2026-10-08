using JoFit.Domain.Cities;

namespace JoFit.Application.Features.Cities.Dtos;

public sealed record CityDto(
    long Id,
    string NameAr,
    string NameEn,
    long CountryId,
    bool IsActive)
{
    public static CityDto From(City entity)
        => new(entity.Id, entity.NameAr, entity.NameEn, entity.CountryId, entity.IsActive);
}
