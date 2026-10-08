using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.CourseCategories.Dtos;
using JoFit.Domain.CourseCategories;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.CourseCategories.Commands;

public sealed record CreateCourseCategoryCommand(CourseCategoryValues Values) : IRequest<Result<CourseCategoryDto>>;

public sealed record UpdateCourseCategoryCommand(long Id, CourseCategoryValues Values) : IRequest<Result<CourseCategoryDto>>;

public sealed record DeleteCourseCategoryCommand(long Id) : IRequest<Result<Deleted>>;

public class CreateCourseCategoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCourseCategoryCommand, Result<CourseCategoryDto>>
{
    public async Task<Result<CourseCategoryDto>> Handle(CreateCourseCategoryCommand request, CancellationToken ct)
    {
        var created = CourseCategory.Create(request.Values);
        if (created.IsError)
        {
            return created.Errors;
        }

        var entity = created.Value;
        var repository = unitOfWork.GetRepository<CourseCategory>();
        var slugValue = entity.Slug;
        if (slugValue is not null && await repository.GetTableNoTracking().AnyAsync(x => x.Slug == slugValue, ct))
        {
            return CourseCategoryErrors.SlugTaken;
        }

        await repository.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return CourseCategoryDto.From(entity);
    }
}

public class UpdateCourseCategoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCourseCategoryCommand, Result<CourseCategoryDto>>
{
    public async Task<Result<CourseCategoryDto>> Handle(UpdateCourseCategoryCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<CourseCategory>();
        var entity = await repository.GetTableAsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            return CourseCategoryErrors.NotFound;
        }

        var updated = entity.Update(request.Values);
        if (updated.IsError)
        {
            return updated.Errors;
        }
        var slugValue = entity.Slug;
        if (slugValue is not null && await repository.GetTableNoTracking().AnyAsync(x => x.Slug == slugValue && x.Id != entity.Id, ct))
        {
            return CourseCategoryErrors.SlugTaken;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return CourseCategoryDto.From(entity);
    }
}

public class DeleteCourseCategoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCourseCategoryCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteCourseCategoryCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<CourseCategory>();
        var entity = await repository.GetTableAsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            return CourseCategoryErrors.NotFound;
        }

        var usedCourseCategoryLink = await unitOfWork.GetDbContext().Set<JoFit.Domain.Courses.CourseCategoryLink>().CountAsync(x => x.CourseCategoryId == entity.Id, ct);
        if (usedCourseCategoryLink > 0)
        {
            return Error.Conflict("CourseCategory.InUse", $"This course category is used by {usedCourseCategoryLink} course(s). Move them first.");
        }

        // Records that reference this one add their own in-use check here when they exist.
        repository.DeleteAsync(entity);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}
