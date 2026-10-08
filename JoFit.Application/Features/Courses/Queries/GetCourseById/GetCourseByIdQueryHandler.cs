using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Courses.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Courses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Courses.Queries.GetCourseById;

public class GetCourseByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCourseByIdQuery, Result<CourseDto>>
{
    public async Task<Result<CourseDto>> Handle(GetCourseByIdQuery request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<Course>()
            .GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return CourseErrors.CourseNotFound;
        }

        return CourseDto.From(entity);
    }
}
