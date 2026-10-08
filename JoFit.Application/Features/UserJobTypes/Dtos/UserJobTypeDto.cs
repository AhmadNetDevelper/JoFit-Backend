using JoFit.Domain.UserJobTypes;

namespace JoFit.Application.Features.UserJobTypes.Dtos;

public sealed record UserJobTypeDto(
    long Id,
    string NameAr,
    string NameEn,
    bool IsActive,
    bool IsNormalUser)
{
    public static UserJobTypeDto From(UserJobType entity)
        => new(entity.Id, entity.NameAr, entity.NameEn, entity.IsActive, entity.IsNormalUser);
}
