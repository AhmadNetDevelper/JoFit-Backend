using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Domain.Careers;
using JoFit.Domain.Common.Results;
using JoFit.Domain.JobCategories;
using JoFit.Domain.Locations;
using JoFit.Domain.Sports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Careers;

public sealed record JobFilters(
    IReadOnlyList<string>? CategorySlugs, IReadOnlyList<string>? CityIds, IReadOnlyList<string>? EmploymentTypes, IReadOnlyList<string>? WorkModes,
    IReadOnlyList<string>? ExperienceLevels, IReadOnlyList<string>? SportSlugs, string? EmployerSlug, string? Search);

public sealed record GetPublicJobsQuery(string Language, int Page, int PageSize, JobFilters Filters, string? Sort) : IRequest<Result<JobListResultDto>>;

public sealed record GetPublicJobBySlugQuery(string Slug, string Language) : IRequest<Result<JobPostingDto>>;

public sealed record GetPublicJobCategoriesQuery(string Language) : IRequest<Result<List<PublicJobCategoryDto>>>;

public sealed record GetPublicCitiesQuery(string Language) : IRequest<Result<List<CityDto>>>;

public sealed record GetPublicEmployersQuery(string Language, int Page, int PageSize, string? Search, string? Sort) : IRequest<Result<PagedResult<EmployerSummaryDto>>>;

public sealed record GetPublicEmployerBySlugQuery(string Slug, string Language) : IRequest<Result<EmployerProfileDto>>;

public sealed record GetCareersStatsQuery : IRequest<Result<CareersStatsDto>>;

/// <summary>Everything the public board needs about one job, loaded in one go so filters, facets and sorting can work in memory.</summary>
internal sealed record BoardJob(Job Job, Employer Employer, string CategorySlug, string CitySlug, IReadOnlyList<string> SportSlugs, int Hires);

internal static class PublicBoard
{
    /// <summary>
    /// Jobs a visitor may read on the board: open, from a verified employer, and not past their closing date (Amman).
    /// A filled job closes by itself, so it never shows.
    /// </summary>
    public static async Task<List<BoardJob>> LoadAsync(IUnitOfWork uow, bool includeClosedOfVerified, CancellationToken ct)
    {
        var db = uow.GetDbContext();
        var today = CareersSupport.Today;
        var jobs = await db.Set<Job>().AsNoTracking()
            .Where(j => (j.Status == JobStates.Open || (includeClosedOfVerified && j.Status == JobStates.Closed))
                        && db.Set<Employer>().Any(e => e.Id == j.EmployerId && e.VerificationStatus == EmployerVerification.Verified))
            .ToListAsync(ct);
        if (!includeClosedOfVerified)
        {
            jobs = jobs.Where(j => j.ClosesOn is null || j.ClosesOn >= today).ToList();
        }

        var ids = jobs.Select(j => j.Id).ToList();
        var employerIds = jobs.Select(j => j.EmployerId).Distinct().ToList();
        var employers = await db.Set<Employer>().AsNoTracking().Where(e => employerIds.Contains(e.Id)).ToDictionaryAsync(e => e.Id, ct);
        var categories = await db.Set<JobCategory>().AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Slug, ct);
        var locations = await CareersSupport.LocationsByIdAsync(uow, ct);
        var sportLinks = await db.Set<JobSportLink>().AsNoTracking().Where(l => ids.Contains(l.JobId)).ToListAsync(ct);
        var sportIds = sportLinks.Select(l => l.SportId).Distinct().ToList();
        var sports = await db.Set<Sport>().AsNoTracking().Where(s => sportIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.Slug ?? string.Empty, ct);
        var hires = await db.Set<JobApplication>().AsNoTracking().Where(a => ids.Contains(a.JobId) && a.Status == ApplicationStatuses.Hired)
            .GroupBy(a => a.JobId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        return jobs.Select(j => new BoardJob(
            j, employers[j.EmployerId], categories.GetValueOrDefault(j.CategoryId) ?? string.Empty, locations.GetValueOrDefault(j.LocationId)?.Slug ?? string.Empty,
            sportLinks.Where(l => l.JobId == j.Id).Select(l => sports.GetValueOrDefault(l.SportId) ?? string.Empty).ToList(), hires.GetValueOrDefault(j.Id))).ToList();
    }

    /// <summary>
    /// Applies the filters. <paramref name="skip"/> leaves one dimension out, which is how each facet's counts ignore
    /// their own filter and apply all the others.
    /// </summary>
    public static IEnumerable<BoardJob> Filter(IEnumerable<BoardJob> jobs, JobFilters f, string? skip = null)
    {
        static bool Has(IReadOnlyList<string>? list) => list is { Count: > 0 };
        if (skip != "categories" && Has(f.CategorySlugs)) jobs = jobs.Where(j => f.CategorySlugs!.Contains(j.CategorySlug));
        if (skip != "cities" && Has(f.CityIds)) jobs = jobs.Where(j => f.CityIds!.Contains(j.CitySlug));
        if (skip != "employmentTypes" && Has(f.EmploymentTypes)) jobs = jobs.Where(j => f.EmploymentTypes!.Contains(j.Job.EmploymentType));
        if (skip != "workModes" && Has(f.WorkModes)) jobs = jobs.Where(j => f.WorkModes!.Contains(j.Job.WorkMode));
        if (skip != "experienceLevels" && Has(f.ExperienceLevels)) jobs = jobs.Where(j => f.ExperienceLevels!.Contains(j.Job.ExperienceLevel));
        if (Has(f.SportSlugs)) jobs = jobs.Where(j => j.SportSlugs.Any(s => f.SportSlugs!.Contains(s)));
        if (!string.IsNullOrWhiteSpace(f.EmployerSlug)) jobs = jobs.Where(j => j.Employer.Slug == f.EmployerSlug);
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var term = f.Search.Trim();
            bool Match(string? text) => text is not null && text.Contains(term, StringComparison.OrdinalIgnoreCase);
            jobs = jobs.Where(j => Match(j.Job.Title) || Match(j.Job.Description) || Match(j.Employer.Name) || j.Job.Requirements.Any(Match));
        }

        return jobs;
    }

    public static IEnumerable<BoardJob> Sort(IEnumerable<BoardJob> jobs, string? sort) => sort switch
    {
        // closing-soon: soonest date first, jobs without one last, then newest.
        "closing-soon" => jobs.OrderBy(j => j.Job.ClosesOn is null ? 1 : 0).ThenBy(j => j.Job.ClosesOn).ThenByDescending(j => j.Job.PostedAtUtc),
        // salary-desc: highest monthly pay first (hourly x 160), jobs without a salary last.
        "salary-desc" => jobs.OrderBy(j => CareersSupport.Salary(j.Job) is null ? 1 : 0).ThenByDescending(j => CareersSupport.MonthlyMax(j.Job)),
        // newest: featured first, then the latest posting.
        _ => jobs.OrderByDescending(j => j.Job.Featured).ThenByDescending(j => j.Job.PostedAtUtc)
    };

    public static List<FacetItem> Facet(IEnumerable<BoardJob> jobs, IEnumerable<(string Value, string? Label)> options, Func<BoardJob, string> key)
    {
        var counts = jobs.GroupBy(key).ToDictionary(g => g.Key, g => g.Count());
        return options.Select(o => new FacetItem(o.Value, o.Label, counts.GetValueOrDefault(o.Value))).ToList();
    }

    public static async Task<List<JobPostingDto>> MapAsync(
        IUnitOfWork uow, ILocalizationService localization, string language, IReadOnlyList<BoardJob> rows, CancellationToken ct)
    {
        var db = uow.GetDbContext();
        var today = CareersSupport.Today;
        var categories = await db.Set<JobCategory>().AsNoTracking().ToDictionaryAsync(c => c.Slug, ct);
        var locations = (await CareersSupport.LocationsByIdAsync(uow, ct)).Values.ToDictionary(l => l.Slug);
        var sports = await db.Set<Sport>().AsNoTracking().Where(s => s.Slug != null).ToDictionaryAsync(s => s.Slug!, ct);

        return rows.Select(r =>
        {
            var j = r.Job;
            var category = categories.GetValueOrDefault(r.CategorySlug);
            var location = locations.GetValueOrDefault(r.CitySlug);
            return new JobPostingDto(
                j.Id, j.Reference, j.Slug, j.Title, j.Description, j.Requirements, j.Benefits, j.LanguageList, j.Positions, j.EmploymentType, j.WorkMode, j.ExperienceLevel,
                j.ShowSalary ? CareersSupport.Salary(j) : null,
                category is null ? null : new JobCategoryRefDto(category.Id, category.Slug, localization.GetLocalizedName(category.NameAr, category.NameEn, language)),
                location is null ? null : new CityDto(location.Slug, localization.GetLocalizedName(location.NameAr, location.NameEn, language)),
                r.SportSlugs.Select(s => sports.GetValueOrDefault(s)).Where(s => s is not null)
                    .Select(s => new JobSportRefDto(s!.Id, s.Slug!, localization.GetLocalizedName(s.NameAr, s.NameEn, language))).ToList(),
                new EmployerRefDto(r.Employer.Id, r.Employer.Slug, r.Employer.Name, r.Employer.Kind, CareersSupport.Logo(r.Employer)),
                j.ClosesOn?.ToString("yyyy-MM-dd"), j.PostedAtUtc, j.Featured,
                j.IsPublic(r.Employer.IsVerified, today) && r.Hires < j.Positions);
        }).ToList();
    }
}

public class GetPublicJobsQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization) : IRequestHandler<GetPublicJobsQuery, Result<JobListResultDto>>
{
    public async Task<Result<JobListResultDto>> Handle(GetPublicJobsQuery request, CancellationToken ct)
    {
        var all = await PublicBoard.LoadAsync(unitOfWork, false, ct);
        var f = request.Filters;

        // Items: every filter applied, sorted, then paged (the last page answers when the page is past the end).
        var items = PublicBoard.Sort(PublicBoard.Filter(all, f), request.Sort).ToList();
        var pageSize = Math.Clamp(request.PageSize, 1, PagingExtensions.PublicMaxPageSize);
        var totalPages = Math.Max(1, (int)Math.Ceiling(items.Count / (double)pageSize));
        var page = Math.Clamp(request.Page, 1, totalPages);
        var slice = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        // Facets: each one ignores its own filter and applies the others; every option is listed, zero counts included.
        var db = unitOfWork.GetDbContext();
        var categories = await db.Set<JobCategory>().AsNoTracking().OrderBy(c => c.NameEn).ToListAsync(ct);
        var locations = (await CareersSupport.LocationsByIdAsync(unitOfWork, ct)).Values.OrderBy(l => l.Id).ToList();
        string Label(string ar, string en) => localization.GetLocalizedName(ar, en, request.Language);

        var facets = new JobFacets(
            PublicBoard.Facet(PublicBoard.Filter(all, f, "categories"), categories.Select(c => (c.Slug, (string?)Label(c.NameAr, c.NameEn))), j => j.CategorySlug),
            PublicBoard.Facet(PublicBoard.Filter(all, f, "cities"), locations.Select(l => (l.Slug, (string?)Label(l.NameAr, l.NameEn))), j => j.CitySlug),
            PublicBoard.Facet(PublicBoard.Filter(all, f, "employmentTypes"), CareersRules.EmploymentTypes.Select(v => (v, (string?)null)), j => j.Job.EmploymentType),
            PublicBoard.Facet(PublicBoard.Filter(all, f, "workModes"), CareersRules.WorkModes.Select(v => (v, (string?)null)), j => j.Job.WorkMode),
            PublicBoard.Facet(PublicBoard.Filter(all, f, "experienceLevels"), CareersRules.ExperienceLevels.Select(v => (v, (string?)null)), j => j.Job.ExperienceLevel));

        var dtos = await PublicBoard.MapAsync(unitOfWork, localization, request.Language, slice, ct);
        var meta = new PagedResult<JobPostingDto>(dtos, page, pageSize, items.Count).Meta;
        return new JobListResultDto(dtos, meta, facets);
    }
}

public class GetPublicJobBySlugQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization) : IRequestHandler<GetPublicJobBySlugQuery, Result<JobPostingDto>>
{
    public async Task<Result<JobPostingDto>> Handle(GetPublicJobBySlugQuery request, CancellationToken ct)
    {
        // A closed job of a verified employer stays readable (it simply no longer takes applications);
        // drafts, reviews and rejections are never readable.
        var all = await PublicBoard.LoadAsync(unitOfWork, true, ct);
        var row = all.FirstOrDefault(r => r.Job.Slug == request.Slug);
        return row is null ? CareersErrors.JobNotFound : (await PublicBoard.MapAsync(unitOfWork, localization, request.Language, [row], ct))[0];
    }
}

public class GetPublicJobCategoriesQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicJobCategoriesQuery, Result<List<PublicJobCategoryDto>>>
{
    public async Task<Result<List<PublicJobCategoryDto>>> Handle(GetPublicJobCategoriesQuery request, CancellationToken ct)
    {
        var all = await PublicBoard.LoadAsync(unitOfWork, false, ct);
        var categories = await unitOfWork.GetDbContext().Set<JobCategory>().AsNoTracking().ToListAsync(ct);
        return categories
            .Select(c => new PublicJobCategoryDto(c.Id, c.Slug, localization.GetLocalizedName(c.NameAr, c.NameEn, request.Language), all.Count(j => j.CategorySlug == c.Slug)))
            .OrderBy(c => c.Name, StringComparer.CurrentCulture).ToList();
    }
}

public class GetPublicCitiesQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization) : IRequestHandler<GetPublicCitiesQuery, Result<List<CityDto>>>
{
    public async Task<Result<List<CityDto>>> Handle(GetPublicCitiesQuery request, CancellationToken ct)
        => (await unitOfWork.GetRepository<Location>().GetTableNoTracking().OrderBy(l => l.Id).ToListAsync(ct))
            .Select(l => new CityDto(l.Slug, localization.GetLocalizedName(l.NameAr, l.NameEn, request.Language))).ToList();
}

public class GetPublicEmployersQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicEmployersQuery, Result<PagedResult<EmployerSummaryDto>>>
{
    public async Task<Result<PagedResult<EmployerSummaryDto>>> Handle(GetPublicEmployersQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var board = await PublicBoard.LoadAsync(unitOfWork, false, ct);

        // Every verified employer is listed, including those with no open jobs.
        var employers = await db.Set<Employer>().AsNoTracking().Where(e => e.VerificationStatus == EmployerVerification.Verified).ToListAsync(ct);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            employers = employers.Where(e => e.Name.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var locations = await CareersSupport.LocationsByIdAsync(unitOfWork, ct);
        var rows = employers.Select(e =>
        {
            var location = locations.GetValueOrDefault(e.LocationId);
            return new EmployerSummaryDto(
                e.Id, e.Slug, e.Name, e.Kind, location is null ? null : new CityDto(location.Slug, localization.GetLocalizedName(location.NameAr, location.NameEn, request.Language)),
                CareersSupport.Logo(e), board.Count(j => j.Employer.Id == e.Id));
        });

        // Default order: most open jobs first, then by name.
        var ordered = rows.OrderByDescending(r => r.OpenJobCount).ThenBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        var pageSize = Math.Clamp(request.PageSize, 1, PagingExtensions.PublicMaxPageSize);
        var totalPages = Math.Max(1, (int)Math.Ceiling(ordered.Count / (double)pageSize));
        var page = Math.Clamp(request.Page, 1, totalPages);
        return new PagedResult<EmployerSummaryDto>(ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, ordered.Count);
    }
}

public class GetPublicEmployerBySlugQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicEmployerBySlugQuery, Result<EmployerProfileDto>>
{
    public async Task<Result<EmployerProfileDto>> Handle(GetPublicEmployerBySlugQuery request, CancellationToken ct)
    {
        var employer = await unitOfWork.GetRepository<Employer>().GetTableNoTracking()
            .FirstOrDefaultAsync(e => e.Slug == request.Slug && e.VerificationStatus == EmployerVerification.Verified, ct);
        if (employer is null)
        {
            return CareersErrors.EmployerNotFound;
        }

        var board = (await PublicBoard.LoadAsync(unitOfWork, false, ct)).Where(j => j.Employer.Id == employer.Id).ToList();
        var postings = await PublicBoard.MapAsync(unitOfWork, localization, request.Language, PublicBoard.Sort(board, null).ToList(), ct);
        var location = (await CareersSupport.LocationsByIdAsync(unitOfWork, ct)).GetValueOrDefault(employer.LocationId);
        return new EmployerProfileDto(
            employer.Id, employer.Slug, employer.Name, employer.Kind,
            location is null ? null : new CityDto(location.Slug, localization.GetLocalizedName(location.NameAr, location.NameEn, request.Language)),
            CareersSupport.Logo(employer), employer.About, employer.Website, employer.Size, postings.Count, postings);
    }
}

public class GetCareersStatsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetCareersStatsQuery, Result<CareersStatsDto>>
{
    public async Task<Result<CareersStatsDto>> Handle(GetCareersStatsQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var board = await PublicBoard.LoadAsync(unitOfWork, false, ct);
        return new CareersStatsDto(
            board.Count,
            await db.Set<Employer>().CountAsync(e => e.VerificationStatus == EmployerVerification.Verified, ct),
            await db.Set<JobCategory>().CountAsync(ct),
            await db.Set<Location>().CountAsync(ct));
    }
}

/// <summary>A narrow door to the public board for other features (the sitemap).</summary>
public static class PublicBoardAccess
{
    public sealed record PublicJobRow(Job Job, Employer Employer);

    public static async Task<List<PublicJobRow>> LoadAsync(IUnitOfWork uow, CancellationToken ct)
        => (await PublicBoard.LoadAsync(uow, false, ct)).Select(b => new PublicJobRow(b.Job, b.Employer)).ToList();
}
