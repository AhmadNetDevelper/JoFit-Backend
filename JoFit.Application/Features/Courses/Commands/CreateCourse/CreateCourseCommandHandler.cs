using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Courses.Dtos;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Courses;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.Courses.Commands.CreateCourse;

public class CreateCourseCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<CreateCourseCommand, Result<CourseDto>>
{
    public async Task<Result<CourseDto>> Handle(CreateCourseCommand request, CancellationToken ct)
    {
        var createResult = Course.Create(request.NameAr, request.NameEn, request.IsActive);
        if (createResult.IsError)
        {
            return createResult.Errors;
        }

        var entity = createResult.Value;

        await unitOfWork.GetRepository<Course>().AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(CourseCacheTags.Courses, ct);

        return CourseDto.From(entity);
    }
}
