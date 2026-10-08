using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.AcademicDegrees.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.AcademicDegrees;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.AcademicDegrees.Commands.CreateAcademicDegree;

public class CreateAcademicDegreeCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<CreateAcademicDegreeCommand, Result<AcademicDegreeDto>>
{
    public async Task<Result<AcademicDegreeDto>> Handle(CreateAcademicDegreeCommand request, CancellationToken ct)
    {
        var createResult = AcademicDegree.Create(request.NameAr, request.NameEn, request.IsActive);
        if (createResult.IsError)
        {
            return createResult.Errors;
        }

        var entity = createResult.Value;

        await unitOfWork.GetRepository<AcademicDegree>().AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(AcademicDegreeCacheTags.AcademicDegrees, ct);

        return AcademicDegreeDto.From(entity);
    }
}
