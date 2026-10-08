using JoFit.Domain.Countries;

namespace JoFit.Application.Features.Countries.Dtos;

public sealed record CountryDto(
    long Id,
    string NameAr,
    string NameEn,
    bool IsActive)
{
    public static CountryDto From(Country entity)
        => new(entity.Id, entity.NameAr, entity.NameEn, entity.IsActive);
}
