using JoFit.Domain.JobStatuses;

namespace JoFit.Application.Features.JobStatuses.Dtos;

public sealed record JobStatusDto(
    long Id,
    string NameAr,
    string NameEn,
    bool IsActive)
{
    public static JobStatusDto From(JobStatus entity)
        => new(entity.Id, entity.NameAr, entity.NameEn, entity.IsActive);
}
