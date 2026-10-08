using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.UniversityMajors.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.UniversityMajors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.UniversityMajors.Queries.GetAllUniversityMajors;

public class GetAllUniversityMajorsQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localizationService)
    : IRequestHandler<GetAllUniversityMajorsQuery, Result<List<UniversityMajorLookupDto>>>
{
    public async Task<Result<List<UniversityMajorLookupDto>>> Handle(GetAllUniversityMajorsQuery request, CancellationToken ct)
    {
        var rows = await unitOfWork.GetRepository<UniversityMajor>()
            .GetTableNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.NameAr, x.NameEn })
            .ToListAsync(ct);

        return rows
            .ConvertAll(x => new UniversityMajorLookupDto(
                x.Id,
                localizationService.GetLocalizedName(x.NameAr, x.NameEn, request.Language)));
    }
}
