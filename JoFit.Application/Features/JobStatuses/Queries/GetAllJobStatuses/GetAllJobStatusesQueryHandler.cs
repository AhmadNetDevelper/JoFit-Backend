using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.JobStatuses.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.JobStatuses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.JobStatuses.Queries.GetAllJobStatuses;

public class GetAllJobStatusesQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localizationService)
    : IRequestHandler<GetAllJobStatusesQuery, Result<List<JobStatusLookupDto>>>
{
    public async Task<Result<List<JobStatusLookupDto>>> Handle(GetAllJobStatusesQuery request, CancellationToken ct)
    {
        var rows = await unitOfWork.GetRepository<JobStatus>()
            .GetTableNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.NameAr, x.NameEn })
            .ToListAsync(ct);

        return rows
            .ConvertAll(x => new JobStatusLookupDto(
                x.Id,
                localizationService.GetLocalizedName(x.NameAr, x.NameEn, request.Language)));
    }
}
