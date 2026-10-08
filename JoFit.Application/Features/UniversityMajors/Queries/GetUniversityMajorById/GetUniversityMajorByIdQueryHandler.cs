using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.UniversityMajors.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.UniversityMajors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.UniversityMajors.Queries.GetUniversityMajorById;

public class GetUniversityMajorByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetUniversityMajorByIdQuery, Result<UniversityMajorDto>>
{
    public async Task<Result<UniversityMajorDto>> Handle(GetUniversityMajorByIdQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<UniversityMajor>()
            .GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return UniversityMajorErrors.UniversityMajorNotFound;
        }

        return UniversityMajorDto.From(entity);
    }
}
