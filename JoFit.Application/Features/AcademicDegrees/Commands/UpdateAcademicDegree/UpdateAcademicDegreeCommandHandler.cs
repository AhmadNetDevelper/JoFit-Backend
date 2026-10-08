using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.AcademicDegrees.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.AcademicDegrees;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.AcademicDegrees.Commands.UpdateAcademicDegree;

public class UpdateAcademicDegreeCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<UpdateAcademicDegreeCommand, Result<AcademicDegreeDto>>
{
    public async Task<Result<AcademicDegreeDto>> Handle(UpdateAcademicDegreeCommand request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<AcademicDegree>()
            .GetTableAsTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return AcademicDegreeErrors.AcademicDegreeNotFound;
        }

        var updateResult = entity.Update(request.NameAr, request.NameEn, request.IsActive);
        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(AcademicDegreeCacheTags.AcademicDegrees, ct);

        return AcademicDegreeDto.From(entity);
    }
}
