using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.UserJobTypes.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.UserJobTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.UserJobTypes.Queries.GetAllUserJobTypes;

public class GetAllUserJobTypesQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localizationService)
    : IRequestHandler<GetAllUserJobTypesQuery, Result<List<UserJobTypeLookupDto>>>
{
    public async Task<Result<List<UserJobTypeLookupDto>>> Handle(GetAllUserJobTypesQuery request, CancellationToken ct)
    {
        var rows = await unitOfWork.GetRepository<UserJobType>()
            .GetTableNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive && !x.IsNormalUser)
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.NameAr, x.NameEn })
            .ToListAsync(ct);

        return rows
            .ConvertAll(x => new UserJobTypeLookupDto(
                x.Id,
                localizationService.GetLocalizedName(x.NameAr, x.NameEn, request.Language)));
    }
}
