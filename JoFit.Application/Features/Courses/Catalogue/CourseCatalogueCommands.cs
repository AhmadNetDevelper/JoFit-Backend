using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Services;
using JoFit.Application.Common.Utilities;
using JoFit.Domain.Common.Results;
using JoFit.Application.Features.Enrolments;
using JoFit.Domain.Courses;
using JoFit.Domain.CourseCategories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Courses.Catalogue;

/// <summary>A course form as saved. Media is null when the key was absent (keep), a list otherwise (replace).</summary>
public sealed record CourseDraftInput(
    CourseDetails Details,
    IReadOnlyList<long> CategoryIds,
    IReadOnlyList<GalleryItem>? Media,
    IReadOnlyList<long>? InstructorIds = null);

public sealed record CreateCourseDraftCommand(CourseDraftInput Input) : IRequest<Result<AdminCourseDto>>;

public sealed record UpdateCourseDetailsCommand(long Id, CourseDraftInput Input) : IRequest<Result<AdminCourseDto>>;

public sealed record SetCourseStatusCommand(long Id, string Status) : IRequest<Result<AdminCourseDto>>;

internal static class CourseSaving
{
    public const string OwnerType = "course";
    public const string MediaField = "media";

    /// <summary>Checks that every category exists; returns the error keyed on categoryIds when not.</summary>
    public static async Task<Error?> CheckCategoriesAsync(IUnitOfWork unitOfWork, IReadOnlyList<long> categoryIds, CancellationToken ct)
    {
        var ids = categoryIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return CourseErrors.Field("categoryIds", "Choose at least one category.");
        }

        var found = await unitOfWork.GetRepository<CourseCategory>().GetTableNoTracking().CountAsync(c => ids.Contains(c.Id), ct);
        return found == ids.Count ? null : CourseErrors.Field("categoryIds", "One of the categories does not exist.");
    }

    /// <summary>Every instructor must be an existing coach.</summary>
    public static async Task<Error?> CheckInstructorsAsync(IUnitOfWork unitOfWork, IReadOnlyList<long>? instructorIds, CancellationToken ct)
    {
        var ids = (instructorIds ?? []).Distinct().ToList();
        if (ids.Count == 0)
        {
            return null;
        }

        var found = await unitOfWork.GetRepository<JoFit.Domain.Coaches.Coach>().GetTableNoTracking().CountAsync(c => ids.Contains(c.Id), ct);
        return found == ids.Count ? null : CourseErrors.Field("instructorIds", "One of the instructors is not a coach.");
    }

    public static async Task ReplaceInstructorsAsync(IUnitOfWork unitOfWork, long courseId, IReadOnlyList<long>? instructorIds, CancellationToken ct)
    {
        // Absent (null) keeps the instructors; a list (even empty) replaces them.
        if (instructorIds is null)
        {
            return;
        }

        var db = unitOfWork.GetDbContext();
        db.RemoveRange(await db.Set<CourseInstructorLink>().Where(l => l.CourseId == courseId).ToListAsync(ct));
        db.AddRange(instructorIds.Distinct().Select(id => new CourseInstructorLink { CourseId = courseId, CoachId = id }));
    }

    public static async Task ReplaceCategoriesAsync(IUnitOfWork unitOfWork, long courseId, IReadOnlyList<long> categoryIds, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var existing = await db.Set<CourseCategoryLink>().Where(l => l.CourseId == courseId).ToListAsync(ct);
        db.RemoveRange(existing);
        db.AddRange(categoryIds.Distinct().Select(id => new CourseCategoryLink { CourseId = courseId, CourseCategoryId = id }));
    }

    public static async Task<AdminCourseDto> ToDtoAsync(IUnitOfWork unitOfWork, Course course, CancellationToken ct)
    {
        var categoryIds = await unitOfWork.GetDbContext().Set<CourseCategoryLink>().AsNoTracking()
            .Where(l => l.CourseId == course.Id).Select(l => l.CourseCategoryId).OrderBy(id => id).ToListAsync(ct);
        var media = await GalleryService.LoadAsync(unitOfWork, OwnerType, MediaField, [course.Id], ct);
        var seats = (await CourseSeats.TakenAsync(unitOfWork, [course], ct))[course.Id];
        var counts = await CourseSeats.EnrolmentCountsAsync(unitOfWork, [course.Id], ct);
        var instructorIds = await unitOfWork.GetDbContext().Set<CourseInstructorLink>().AsNoTracking()
            .Where(l => l.CourseId == course.Id).Select(l => l.CoachId).OrderBy(id => id).ToListAsync(ct);
        return AdminCourseDto.From(course, categoryIds, media.GetValueOrDefault(course.Id) ?? [], seats, counts.GetValueOrDefault(course.Id), instructorIds);
    }
}

public class CreateCourseDraftCommandHandler(IUnitOfWork unitOfWork, IUrlService urlService)
    : IRequestHandler<CreateCourseDraftCommand, Result<AdminCourseDto>>
{
    public async Task<Result<AdminCourseDto>> Handle(CreateCourseDraftCommand request, CancellationToken ct)
    {
        var input = request.Input;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var created = Course.CreateDraft(input.Details, today);
        var errors = created.IsError ? created.Errors : [];
        if (await CourseSaving.CheckCategoriesAsync(unitOfWork, input.CategoryIds, ct) is Error categoryError)
        {
            errors.Add(categoryError);
        }

        if (await CourseSaving.CheckInstructorsAsync(unitOfWork, input.InstructorIds, ct) is Error instructorError)
        {
            errors.Add(instructorError);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var course = created.Value;
        var repository = unitOfWork.GetRepository<Course>();
        var taken = await repository.GetTableNoTracking().Where(c => c.Slug != null).Select(c => c.Slug!).ToListAsync(ct);
        var taken2 = taken.ToHashSet();
        var baseSlug = Slugger.Slugify(string.IsNullOrWhiteSpace(input.Details.Slug) ? input.Details.TitleEn : input.Details.Slug);
        if (!string.IsNullOrWhiteSpace(input.Details.Slug) && taken2.Contains(baseSlug))
        {
            return CourseErrors.SlugTaken;
        }

        course.ApplyDetails(input.Details with { Slug = Slugger.Unique(baseSlug.Length == 0 ? "course" : baseSlug, taken2.Contains) }, today, intakeChanged: true);

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await repository.AddAsync(course, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await CourseSaving.ReplaceCategoriesAsync(unitOfWork, course.Id, input.CategoryIds, ct);
            await CourseSaving.ReplaceInstructorsAsync(unitOfWork, course.Id, input.InstructorIds, ct);

            if (input.Media is not null)
            {
                var gallery = await GalleryService.SyncAsync(unitOfWork, urlService, CourseSaving.OwnerType, course.Id, CourseSaving.MediaField, input.Media, ct);
                if (gallery.IsError)
                {
                    await unitOfWork.RollbackAsync(ct);
                    return gallery.Errors;
                }
            }

            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }

        return await CourseSaving.ToDtoAsync(unitOfWork, course, ct);
    }
}

public class UpdateCourseDetailsCommandHandler(IUnitOfWork unitOfWork, IUrlService urlService)
    : IRequestHandler<UpdateCourseDetailsCommand, Result<AdminCourseDto>>
{
    public async Task<Result<AdminCourseDto>> Handle(UpdateCourseDetailsCommand request, CancellationToken ct)
    {
        var input = request.Input;
        var repository = unitOfWork.GetRepository<Course>();
        var course = await repository.GetTableAsTracking().FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (course is null)
        {
            return CourseErrors.CourseNotFound;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var intakeChanged = input.Details.IntakeStartsOn != course.IntakeStartsOn;
        var seatsTaken = (await CourseSeats.TakenAsync(unitOfWork, [course], ct))[course.Id];
        var details = input.Details.Slug is null ? input.Details with { Slug = course.Slug } : input.Details;

        if (!string.IsNullOrWhiteSpace(details.Slug))
        {
            var slug = details.Slug.Trim();
            if (await repository.GetTableNoTracking().AnyAsync(c => c.Slug == slug && c.Id != course.Id, ct))
            {
                return CourseErrors.SlugTaken;
            }
        }

        var applied = course.ApplyDetails(details, today, intakeChanged, seatsTaken);
        var errors = applied.IsError ? applied.Errors : [];
        if (await CourseSaving.CheckCategoriesAsync(unitOfWork, input.CategoryIds, ct) is Error categoryError)
        {
            errors.Add(categoryError);
        }

        if (await CourseSaving.CheckInstructorsAsync(unitOfWork, input.InstructorIds, ct) is Error instructorError)
        {
            errors.Add(instructorError);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await CourseSaving.ReplaceCategoriesAsync(unitOfWork, course.Id, input.CategoryIds, ct);
            await CourseSaving.ReplaceInstructorsAsync(unitOfWork, course.Id, input.InstructorIds, ct);
            if (input.Media is not null)
            {
                var gallery = await GalleryService.SyncAsync(unitOfWork, urlService, CourseSaving.OwnerType, course.Id, CourseSaving.MediaField, input.Media, ct);
                if (gallery.IsError)
                {
                    await unitOfWork.RollbackAsync(ct);
                    return gallery.Errors;
                }
            }

            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }

        return await CourseSaving.ToDtoAsync(unitOfWork, course, ct);
    }
}

public class SetCourseStatusCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<SetCourseStatusCommand, Result<AdminCourseDto>>
{
    public async Task<Result<AdminCourseDto>> Handle(SetCourseStatusCommand request, CancellationToken ct)
    {
        var course = await unitOfWork.GetRepository<Course>().GetTableAsTracking().FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (course is null)
        {
            return CourseErrors.CourseNotFound;
        }

        var result = course.SetStatus(request.Status);
        if (result.IsError)
        {
            return result.Errors;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return await CourseSaving.ToDtoAsync(unitOfWork, course, ct);
    }
}
