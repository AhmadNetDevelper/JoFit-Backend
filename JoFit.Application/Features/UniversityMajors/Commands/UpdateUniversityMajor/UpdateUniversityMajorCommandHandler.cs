using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.UniversityMajors.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.UniversityMajors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.UniversityMajors.Commands.UpdateUniversityMajor;

public class UpdateUniversityMajorCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<UpdateUniversityMajorCommand, Result<UniversityMajorDto>>
{
    public async Task<Result<UniversityMajorDto>> Handle(UpdateUniversityMajorCommand request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<UniversityMajor>()
            .GetTableAsTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return UniversityMajorErrors.UniversityMajorNotFound;
        }

        var updateResult = entity.Update(request.NameAr, request.NameEn, request.IsActive);
        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(UniversityMajorCacheTags.UniversityMajors, ct);

        return UniversityMajorDto.From(entity);
    }
}
