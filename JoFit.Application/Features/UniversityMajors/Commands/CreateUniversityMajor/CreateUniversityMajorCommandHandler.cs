using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.UniversityMajors.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.UniversityMajors;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.UniversityMajors.Commands.CreateUniversityMajor;

public class CreateUniversityMajorCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<CreateUniversityMajorCommand, Result<UniversityMajorDto>>
{
    public async Task<Result<UniversityMajorDto>> Handle(CreateUniversityMajorCommand request, CancellationToken ct)
    {
        var createResult = UniversityMajor.Create(request.NameAr, request.NameEn, request.IsActive);
        if (createResult.IsError)
        {
            return createResult.Errors;
        }

        var entity = createResult.Value;

        await unitOfWork.GetRepository<UniversityMajor>().AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(UniversityMajorCacheTags.UniversityMajors, ct);

        return UniversityMajorDto.From(entity);
    }
}
