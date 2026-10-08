using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.AcademicDegrees.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.AcademicDegrees;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.AcademicDegrees.Queries.GetAcademicDegreeById;

public class GetAcademicDegreeByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAcademicDegreeByIdQuery, Result<AcademicDegreeDto>>
{
    public async Task<Result<AcademicDegreeDto>> Handle(GetAcademicDegreeByIdQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<AcademicDegree>()
            .GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return AcademicDegreeErrors.AcademicDegreeNotFound;
        }

        return AcademicDegreeDto.From(entity);
    }
}
