using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.AcademicDegrees.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.AcademicDegrees;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.AcademicDegrees.Queries.GetAllAcademicDegrees;

public class GetAllAcademicDegreesQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localizationService)
    : IRequestHandler<GetAllAcademicDegreesQuery, Result<List<AcademicDegreeLookupDto>>>
{
    public async Task<Result<List<AcademicDegreeLookupDto>>> Handle(GetAllAcademicDegreesQuery request, CancellationToken ct)
    {
        var rows = await unitOfWork.GetRepository<AcademicDegree>()
            .GetTableNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.NameAr, x.NameEn })
            .ToListAsync(ct);

        return rows
            .ConvertAll(x => new AcademicDegreeLookupDto(
                x.Id,
                localizationService.GetLocalizedName(x.NameAr, x.NameEn, request.Language)));
    }
}
