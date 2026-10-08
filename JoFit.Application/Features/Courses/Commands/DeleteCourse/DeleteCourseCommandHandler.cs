using JoFit.Application.Common.Interfaces;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Courses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace JoFit.Application.Features.Courses.Commands.DeleteCourse;

public class DeleteCourseCommandHandler(IUnitOfWork unitOfWork, HybridCache cache)
    : IRequestHandler<DeleteCourseCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteCourseCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<Course>();

        var entity = await repository.GetTableAsTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (entity is null)
        {
            return CourseErrors.CourseNotFound;
        }

        // A course with pending or active enrolments is archived, not deleted.
        var live = JoFit.Domain.Enrolments.EnrolmentStatuses.Live;
        var enrolments = await unitOfWork.GetRepository<JoFit.Domain.Enrolments.Enrolment>().GetTableNoTracking()
            .CountAsync(e => e.CourseId == entity.Id && live.Contains(e.Status), ct);
        if (enrolments > 0)
        {
            return CourseErrors.HasEnrolments(enrolments);
        }

        // Soft delete: flags IsDeleted and keeps the row
        repository.DeleteAsync(entity);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(CourseCacheTags.Courses, ct);

        return Result.Deleted;
    }
}
