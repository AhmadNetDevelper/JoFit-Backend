using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.JobTypes.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.JobTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.JobTypes.Queries.GetAllJobTypes;

public class GetAllJobTypesQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localizationService)
    : IRequestHandler<GetAllJobTypesQuery, Result<List<JobTypeLookupDto>>>
{
    public async Task<Result<List<JobTypeLookupDto>>> Handle(GetAllJobTypesQuery request, CancellationToken ct)
    {
        var rows = await unitOfWork.GetRepository<JobType>()
            .GetTableNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.NameAr, x.NameEn })
            .ToListAsync(ct);

        return rows
            .ConvertAll(x => new JobTypeLookupDto(
                x.Id,
                localizationService.GetLocalizedName(x.NameAr, x.NameEn, request.Language)));
    }
}
