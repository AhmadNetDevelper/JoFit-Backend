using JoFit.Domain.JobTypes;

namespace JoFit.Application.Features.JobTypes.Dtos;

public sealed record JobTypeDto(
    long Id,
    string NameAr,
    string NameEn,
    bool IsActive)
{
    public static JobTypeDto From(JobType entity)
        => new(entity.Id, entity.NameAr, entity.NameEn, entity.IsActive);
}
