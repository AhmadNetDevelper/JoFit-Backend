using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Services;
using JoFit.Application.Features.Enrolments;
using JoFit.Domain.CourseCategories;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Courses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Courses.Catalogue;

// ---- admin ----------------------------------------------------------------

public sealed record GetAdminCoursesQuery(
    int Page,
    int PageSize,
    string? Search,
    IReadOnlyList<string>? Statuses,
    IReadOnlyList<long>? CategoryIds,
    string? SortField,
    string? SortDir) : IRequest<Result<PagedResult<AdminCourseDto>>>;

public sealed record GetAdminCourseQuery(long Id) : IRequest<Result<AdminCourseDto>>;

public class GetAdminCoursesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdminCoursesQuery, Result<PagedResult<AdminCourseDto>>>
{
    public async Task<Result<PagedResult<AdminCourseDto>>> Handle(GetAdminCoursesQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var query = unitOfWork.GetRepository<Course>().GetTableNoTracking();

        if (request.Statuses is { Count: > 0 })
        {
            var statuses = request.Statuses;
            query = query.Where(c => statuses.Contains(c.Status));
        }

        if (request.CategoryIds is { Count: > 0 })
        {
            var categoryIds = request.CategoryIds;
            query = query.Where(c => db.Set<CourseCategoryLink>().Any(l => l.CourseId == c.Id && categoryIds.Contains(l.CourseCategoryId)));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(c => c.NameEn.ToLower().Contains(term) || c.NameAr.ToLower().Contains(term)
                                     || c.SummaryEn.ToLower().Contains(term) || c.SummaryAr.ToLower().Contains(term)
                                     || (c.Slug != null && c.Slug.Contains(term)));
        }

        var descending = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = request.SortField?.ToLowerInvariant() switch
        {
            "title" => descending ? query.OrderByDescending(c => c.NameEn) : query.OrderBy(c => c.NameEn),
            "status" => descending ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
            "updatedat" => descending
                ? query.OrderByDescending(c => c.LastModifiedUtc ?? c.CreatedAtUtc)
                : query.OrderBy(c => c.LastModifiedUtc ?? c.CreatedAtUtc),
            _ => query.OrderBy(c => c.Id)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
        var ids = page.Items.Select(c => c.Id).ToList();
        var links = await db.Set<CourseCategoryLink>().AsNoTracking().Where(l => ids.Contains(l.CourseId)).ToListAsync(ct);
        var media = await GalleryService.LoadAsync(unitOfWork, "course", "media", ids, ct);

        var seats = await CourseSeats.TakenAsync(unitOfWork, page.Items.ToList(), ct);
        var counts = await CourseSeats.EnrolmentCountsAsync(unitOfWork, ids, ct);
        var instructorLinks = await db.Set<CourseInstructorLink>().AsNoTracking().Where(l => ids.Contains(l.CourseId)).ToListAsync(ct);
        var items = page.Items.Select(c => AdminCourseDto.From(
            c,
            links.Where(l => l.CourseId == c.Id).Select(l => l.CourseCategoryId).OrderBy(x => x).ToList(),
            media.GetValueOrDefault(c.Id) ?? [],
            seats.GetValueOrDefault(c.Id),
            counts.GetValueOrDefault(c.Id),
            instructorLinks.Where(l => l.CourseId == c.Id).Select(l => l.CoachId).OrderBy(x => x).ToList())).ToList();
        return new PagedResult<AdminCourseDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetAdminCourseQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdminCourseQuery, Result<AdminCourseDto>>
{
    public async Task<Result<AdminCourseDto>> Handle(GetAdminCourseQuery request, CancellationToken ct)
    {
        var course = await unitOfWork.GetRepository<Course>().GetTableNoTracking().FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (course is null)
        {
            return CourseErrors.CourseNotFound;
        }

        var categoryIds = await unitOfWork.GetDbContext().Set<CourseCategoryLink>().AsNoTracking()
            .Where(l => l.CourseId == course.Id).Select(l => l.CourseCategoryId).OrderBy(id => id).ToListAsync(ct);
        var media = await GalleryService.LoadAsync(unitOfWork, "course", "media", [course.Id], ct);
        var seatsTaken = (await CourseSeats.TakenAsync(unitOfWork, [course], ct))[course.Id];
        var counts = await CourseSeats.EnrolmentCountsAsync(unitOfWork, [course.Id], ct);
        var instructorIds = await unitOfWork.GetDbContext().Set<CourseInstructorLink>().AsNoTracking()
            .Where(l => l.CourseId == course.Id).Select(l => l.CoachId).OrderBy(id => id).ToListAsync(ct);
        return AdminCourseDto.From(course, categoryIds, media.GetValueOrDefault(course.Id) ?? [], seatsTaken, counts.GetValueOrDefault(course.Id), instructorIds);
    }
}

// ---- storefront -----------------------------------------------------------

public sealed record GetPublicCoursesQuery(
    string Language,
    int Page,
    int PageSize,
    string? Search,
    IReadOnlyList<string>? CategorySlugs,
    IReadOnlyList<string>? Formats,
    IReadOnlyList<string>? Levels,
    IReadOnlyList<string>? Languages,
    long? PriceMin,
    long? PriceMax,
    string? Sort) : IRequest<Result<PagedResult<PublicCourseDto>>>;

public sealed record GetPublicCourseBySlugQuery(string Slug, string Language) : IRequest<Result<PublicCourseDto>>;

public class GetPublicCoursesQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicCoursesQuery, Result<PagedResult<PublicCourseDto>>>
{
    public async Task<Result<PagedResult<PublicCourseDto>>> Handle(GetPublicCoursesQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var arabic = localization.NormalizeLanguage(request.Language) == "ar";
        var query = unitOfWork.GetRepository<Course>().GetTableNoTracking()
            .Where(c => c.Status == CourseStatuses.Published && c.Slug != null);

        if (request.CategorySlugs is { Count: > 0 })
        {
            var slugs = request.CategorySlugs;
            query = query.Where(c => db.Set<CourseCategoryLink>().Any(l => l.CourseId == c.Id
                && db.Set<CourseCategory>().Any(cat => cat.Id == l.CourseCategoryId && cat.Slug != null && slugs.Contains(cat.Slug))));
        }

        if (request.Formats is { Count: > 0 })
        {
            var formats = request.Formats;
            query = query.Where(c => formats.Contains(c.Format));
        }

        if (request.Levels is { Count: > 0 })
        {
            var levels = request.Levels;
            query = query.Where(c => levels.Contains(c.Level));
        }

        if (request.Languages is { Count: > 0 })
        {
            var languages = request.Languages;
            query = query.Where(c => languages.Any(l => ("," + c.Languages + ",").Contains("," + l + ",")));
        }

        if (request.PriceMin is long min) query = query.Where(c => c.PriceAmount >= min);
        if (request.PriceMax is long max) query = query.Where(c => c.PriceAmount <= max);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = arabic
                ? query.Where(c => c.NameAr.ToLower().Contains(term) || c.SummaryAr.ToLower().Contains(term) || (c.DescriptionAr != null && c.DescriptionAr.ToLower().Contains(term)))
                : query.Where(c => c.NameEn.ToLower().Contains(term) || c.SummaryEn.ToLower().Contains(term) || (c.DescriptionEn != null && c.DescriptionEn.ToLower().Contains(term)));
        }

        query = request.Sort switch
        {
            "price-asc" => query.OrderBy(c => c.PriceAmount).ThenBy(c => c.Id),
            "price-desc" => query.OrderByDescending(c => c.PriceAmount).ThenBy(c => c.Id),
            "title" => arabic ? query.OrderBy(c => c.NameAr) : query.OrderBy(c => c.NameEn),
            _ => query.OrderByDescending(c => c.PublishedAtUtc).ThenBy(c => c.Id)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct, PagingExtensions.PublicMaxPageSize);
        var items = await PublicCourseMapper.MapAsync(unitOfWork, localization, request.Language, page.Items, ct);
        return new PagedResult<PublicCourseDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetPublicCourseBySlugQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicCourseBySlugQuery, Result<PublicCourseDto>>
{
    public async Task<Result<PublicCourseDto>> Handle(GetPublicCourseBySlugQuery request, CancellationToken ct)
    {
        // Drafts and archived courses answer not-found.
        var course = await unitOfWork.GetRepository<Course>().GetTableNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == request.Slug && c.Status == CourseStatuses.Published, ct);
        if (course is null)
        {
            return CourseErrors.CourseNotFound;
        }

        return (await PublicCourseMapper.MapAsync(unitOfWork, localization, request.Language, [course], ct))[0];
    }
}

internal static class PublicCourseMapper
{
    public static async Task<List<PublicCourseDto>> MapAsync(
        IUnitOfWork unitOfWork, ILocalizationService localization, string language, IReadOnlyList<Course> courses, CancellationToken ct)
    {
        var ids = courses.Select(c => c.Id).ToList();
        var db = unitOfWork.GetDbContext();
        var links = await db.Set<CourseCategoryLink>().AsNoTracking().Where(l => ids.Contains(l.CourseId)).ToListAsync(ct);
        var categoryIds = links.Select(l => l.CourseCategoryId).Distinct().ToList();
        var categories = await db.Set<CourseCategory>().AsNoTracking().Where(c => categoryIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
        var media = await GalleryService.LoadAsync(unitOfWork, "course", "media", ids, ct);
        var seats = await CourseSeats.TakenAsync(unitOfWork, courses, ct);
        var teachers = await db.Set<CourseInstructorLink>().AsNoTracking().Where(l => ids.Contains(l.CourseId)).ToListAsync(ct);
        var coachIds = teachers.Select(t => t.CoachId).Distinct().ToList();
        var coaches = await db.Set<JoFit.Domain.Coaches.Coach>().AsNoTracking()
            .Where(c => coachIds.Contains(c.Id) && c.VerificationStatus == JoFit.Domain.Coaches.CoachVerification.Approved).ToDictionaryAsync(c => c.Id, ct);

        return courses.Select(c => PublicCourseDto.From(
            c,
            localization,
            language,
            links.Where(l => l.CourseId == c.Id)
                .Select(l => categories.GetValueOrDefault(l.CourseCategoryId))
                .Where(cat => cat is not null)
                .Select(cat => new CourseCategoryRefDto(cat!.Id, cat.Slug ?? string.Empty, localization.GetLocalizedName(cat.NameAr, cat.NameEn, language)))
                .OrderBy(cat => cat.Name)
                .ToList(),
            media.GetValueOrDefault(c.Id) ?? [],
            seatsTaken: seats.GetValueOrDefault(c.Id),
            instructors: teachers.Where(t => t.CourseId == c.Id).Select(t => coaches.GetValueOrDefault(t.CoachId)).Where(x => x is not null)
                .Select(x => new CourseCategoryRefDto(x!.Id, x.Slug, localization.GetLocalizedName(x.NameAr, x.NameEn, language))).ToList())).ToList();
    }
}
