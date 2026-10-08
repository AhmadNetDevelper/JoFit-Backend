using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Courses.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Courses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Courses.Queries.GetAllCourses;

public class GetAllCoursesQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localizationService)
    : IRequestHandler<GetAllCoursesQuery, Result<List<CourseLookupDto>>>
{
    public async Task<Result<List<CourseLookupDto>>> Handle(GetAllCoursesQuery request, CancellationToken ct)
    {
        var rows = await unitOfWork.GetRepository<Course>()
            .GetTableNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.NameAr, x.NameEn })
            .ToListAsync(ct);

        return rows
            .ConvertAll(x => new CourseLookupDto(
                x.Id,
                localizationService.GetLocalizedName(x.NameAr, x.NameEn, request.Language)));
    }
}
