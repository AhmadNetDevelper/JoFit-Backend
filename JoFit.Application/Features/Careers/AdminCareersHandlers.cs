using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Services;
using JoFit.Application.Features.Notifications;
using JoFit.Application.Features.Reviews;
using JoFit.Domain.Careers;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity.Users;
using JoFit.Domain.JobCategories;
using JoFit.Domain.Locations;
using JoFit.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Careers;

// ---- employers ---------------------------------------------------------------------

public sealed record GetAdminEmployersQuery(int Page, int PageSize, string? Search, IReadOnlyList<string>? Statuses, string? SortField, string? SortDir)
    : IRequest<Result<PagedResult<AdminEmployerDto>>>;

public sealed record GetEmployerOptionsQuery : IRequest<Result<List<EmployerOptionDto>>>;

public sealed record GetAdminEmployerQuery(long Id) : IRequest<Result<AdminEmployerDto>>;

public sealed record DecideEmployerVerificationCommand(long Id, string Status, string? Note) : IRequest<Result<AdminEmployerDto>>;

// ---- jobs ----------------------------------------------------------------------------

public sealed record GetAdminJobsQuery(
    int Page, int PageSize, string? Search, IReadOnlyList<string>? Statuses, IReadOnlyList<long>? EmployerIds, IReadOnlyList<long>? CategoryIds,
    string? SortField, string? SortDir) : IRequest<Result<PagedResult<AdminJobDto>>>;

public sealed record GetAdminJobQuery(long Id) : IRequest<Result<AdminJobDto>>;

public sealed record ModerateJobCommand(long Id, string? Decision, string? Note) : IRequest<Result<AdminJobDto>>;

public sealed record SetJobFeaturedCommand(long Id, bool Featured) : IRequest<Result<AdminJobDto>>;

public sealed record CloseJobAsStaffCommand(long Id) : IRequest<Result<AdminJobDto>>;

public sealed record DeleteJobAsStaffCommand(long Id) : IRequest<Result<Deleted>>;

public sealed record GetCareersPendingCountQuery : IRequest<Result<CareersPendingCountDto>>;

// ---- job categories ------------------------------------------------------------------

public sealed record GetAdminJobCategoriesQuery(int Page, int PageSize, string? Search, string? SortField, string? SortDir, string? Locale)
    : IRequest<Result<PagedResult<AdminJobCategoryDto>>>;

public sealed record GetJobCategoryOptionsQuery(string? Locale) : IRequest<Result<List<AdminJobCategoryDto>>>;

public sealed record SaveJobCategoryCommand(long? Id, string? Slug, string NameEn, string NameAr) : IRequest<Result<AdminJobCategoryDto>>;

public sealed record DeleteJobCategoryCommand(long Id) : IRequest<Result<Deleted>>;

internal static class AdminCareersSupport
{
    public static async Task<List<AdminEmployerDto>> EmployersAsync(IUnitOfWork uow, IReadOnlyList<Employer> employers, CancellationToken ct)
    {
        var db = uow.GetDbContext();
        var ids = employers.Select(e => e.Id).ToList();
        var userIds = employers.Select(e => e.UserId).ToList();
        var emails = await db.Set<User>().AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Email, ct);
        var locations = await CareersSupport.LocationsByIdAsync(uow, ct);
        var jobs = await db.Set<Job>().AsNoTracking().Where(j => ids.Contains(j.EmployerId)).Select(j => new { j.Id, j.EmployerId, j.Status, j.ClosesOn }).ToListAsync(ct);
        var jobIds = jobs.Select(j => j.Id).ToList();
        var applications = await db.Set<JobApplication>().AsNoTracking().Where(a => jobIds.Contains(a.JobId)).Select(a => a.JobId).ToListAsync(ct);
        var today = CareersSupport.Today;

        return employers.Select(e =>
        {
            var mine = jobs.Where(j => j.EmployerId == e.Id).ToList();
            var location = locations.GetValueOrDefault(e.LocationId);
            return new AdminEmployerDto(
                e.Id, e.Slug, e.Name, e.Kind, location?.Slug ?? string.Empty, location is null ? null : new LocalizedText(location.NameEn, location.NameAr),
                e.About, e.Website, e.PublicEmail, e.PublicPhone, e.ContactName, e.Size, CareersSupport.Logo(e), e.VerificationStatus, e.VerificationNote,
                emails.GetValueOrDefault(e.UserId) ?? string.Empty, mine.Count,
                mine.Count(j => j.Status == JobStates.Open && e.IsVerified && (j.ClosesOn is null || j.ClosesOn >= today)),
                applications.Count(a => mine.Any(j => j.Id == a)), e.CreatedAtUtc, e.LastModifiedUtc ?? e.CreatedAtUtc);
        }).ToList();
    }

    public static async Task<List<AdminJobDto>> JobsAsync(IUnitOfWork uow, IReadOnlyList<Job> jobs, CancellationToken ct)
    {
        var db = uow.GetDbContext();
        var ids = jobs.Select(j => j.Id).ToList();
        var employerIds = jobs.Select(j => j.EmployerId).Distinct().ToList();
        var employers = await db.Set<Employer>().IgnoreQueryFilters().AsNoTracking().Where(e => employerIds.Contains(e.Id)).ToDictionaryAsync(e => e.Id, ct);
        var categories = await db.Set<JobCategory>().IgnoreQueryFilters().AsNoTracking().ToDictionaryAsync(c => c.Id, ct);
        var locations = await CareersSupport.LocationsByIdAsync(uow, ct);
        var sports = await db.Set<JobSportLink>().AsNoTracking().Where(l => ids.Contains(l.JobId)).ToListAsync(ct);
        var applications = await db.Set<JobApplication>().AsNoTracking().Where(a => ids.Contains(a.JobId)).GroupBy(a => a.JobId)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var today = CareersSupport.Today;

        return jobs.Select(j =>
        {
            var employer = employers[j.EmployerId];
            var category = categories.GetValueOrDefault(j.CategoryId);
            var location = locations.GetValueOrDefault(j.LocationId);
            return new AdminJobDto(
                j.Id, j.Reference, j.Slug, j.Title, employer.Id, employer.Name, employer.VerificationStatus, j.CategoryId,
                category is null ? null : new LocalizedText(category.NameEn, category.NameAr), sports.Where(l => l.JobId == j.Id).Select(l => l.SportId).OrderBy(x => x).ToList(),
                location?.Slug ?? string.Empty, location is null ? null : new LocalizedText(location.NameEn, location.NameAr), j.EmploymentType, j.WorkMode, j.ExperienceLevel,
                CareersSupport.Salary(j), j.ShowSalary, j.Description, j.Requirements, j.Benefits, j.LanguageList, j.Positions, j.ClosesOn?.ToString("yyyy-MM-dd"),
                j.Status, j.ModerationNote, j.Featured, j.IsPublic(employer.IsVerified, today), applications.GetValueOrDefault(j.Id), j.PostedAtUtc,
                j.CreatedAtUtc, j.LastModifiedUtc ?? j.CreatedAtUtc);
        }).ToList();
    }

    public static async Task<List<AdminJobCategoryDto>> CategoriesAsync(IUnitOfWork uow, IReadOnlyList<JobCategory> categories, CancellationToken ct)
    {
        var db = uow.GetDbContext();
        var today = CareersSupport.Today;
        var jobs = await db.Set<Job>().AsNoTracking()
            .Select(j => new { j.CategoryId, j.Status, j.ClosesOn, Verified = db.Set<Employer>().Any(e => e.Id == j.EmployerId && e.VerificationStatus == EmployerVerification.Verified) }).ToListAsync(ct);
        return categories.Select(c =>
        {
            var mine = jobs.Where(j => j.CategoryId == c.Id).ToList();
            return new AdminJobCategoryDto(c.Id, c.Slug ?? string.Empty, new LocalizedText(c.NameEn, c.NameAr), mine.Count,
                mine.Count(j => j.Status == JobStates.Open && j.Verified && (j.ClosesOn is null || j.ClosesOn >= today)));
        }).ToList();
    }
}

public class GetAdminEmployersQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdminEmployersQuery, Result<PagedResult<AdminEmployerDto>>>
{
    public async Task<Result<PagedResult<AdminEmployerDto>>> Handle(GetAdminEmployersQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var query = db.Set<Employer>().AsNoTracking().AsQueryable();
        if (request.Statuses is { Count: > 0 })
        {
            var statuses = request.Statuses;
            query = query.Where(e => statuses.Contains(e.VerificationStatus));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(e => e.Name.ToLower().Contains(term) || e.Slug.Contains(term) || e.ContactName.ToLower().Contains(term)
                || (e.PublicEmail != null && e.PublicEmail.Contains(term)) || (e.PublicPhone != null && e.PublicPhone.Contains(term))
                || db.Set<Location>().Any(l => l.Id == e.LocationId && (l.NameEn.ToLower().Contains(term) || l.NameAr.ToLower().Contains(term)))
                || db.Set<User>().Any(u => u.Id == e.UserId && u.Email!.ToLower().Contains(term)));
        }

        var descending = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        // Default: pending first, then suspended, rejected, verified; newest first inside each. "verificationStatus" sorts by that same rank.
        query = request.SortField?.ToLowerInvariant() switch
        {
            "name" => descending ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name),
            "createdat" => descending ? query.OrderByDescending(e => e.CreatedAtUtc) : query.OrderBy(e => e.CreatedAtUtc),
            "updatedat" => descending ? query.OrderByDescending(e => e.LastModifiedUtc ?? e.CreatedAtUtc) : query.OrderBy(e => e.LastModifiedUtc ?? e.CreatedAtUtc),
            "verificationstatus" => descending
                ? query.OrderByDescending(e => e.VerificationStatus == "pending" ? 0 : e.VerificationStatus == "suspended" ? 1 : e.VerificationStatus == "rejected" ? 2 : 3)
                : query.OrderBy(e => e.VerificationStatus == "pending" ? 0 : e.VerificationStatus == "suspended" ? 1 : e.VerificationStatus == "rejected" ? 2 : 3),
            _ => query.OrderBy(e => e.VerificationStatus == "pending" ? 0 : e.VerificationStatus == "suspended" ? 1 : e.VerificationStatus == "rejected" ? 2 : 3)
                .ThenByDescending(e => e.CreatedAtUtc)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
        return new PagedResult<AdminEmployerDto>(await AdminCareersSupport.EmployersAsync(unitOfWork, page.Items.ToList(), ct), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetEmployerOptionsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetEmployerOptionsQuery, Result<List<EmployerOptionDto>>>
{
    public async Task<Result<List<EmployerOptionDto>>> Handle(GetEmployerOptionsQuery request, CancellationToken ct)
        => (await unitOfWork.GetRepository<Employer>().GetTableNoTracking().OrderBy(e => e.Name).ToListAsync(ct))
            .Select(e => new EmployerOptionDto(e.Id, e.Name, e.VerificationStatus)).ToList();
}

public class GetAdminEmployerQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdminEmployerQuery, Result<AdminEmployerDto>>
{
    public async Task<Result<AdminEmployerDto>> Handle(GetAdminEmployerQuery request, CancellationToken ct)
    {
        var employer = await unitOfWork.GetRepository<Employer>().GetTableNoTracking().FirstOrDefaultAsync(e => e.Id == request.Id, ct);
        return employer is null ? CareersErrors.EmployerNotFound : (await AdminCareersSupport.EmployersAsync(unitOfWork, [employer], ct))[0];
    }
}

public class DecideEmployerVerificationCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DecideEmployerVerificationCommand, Result<AdminEmployerDto>>
{
    public async Task<Result<AdminEmployerDto>> Handle(DecideEmployerVerificationCommand request, CancellationToken ct)
    {
        var employer = await unitOfWork.GetRepository<Employer>().GetTableAsTracking().FirstOrDefaultAsync(e => e.Id == request.Id, ct);
        if (employer is null)
        {
            return CareersErrors.EmployerNotFound;
        }

        var decided = employer.Decide(request.Status, request.Note);
        if (decided.IsError)
        {
            return decided.Errors;
        }

        // Rejecting or suspending takes every job off the board at once, by the public test; the employer is told why.
        var (title, body) = request.Status switch
        {
            EmployerVerification.Verified => (("You are verified", "تم التحقق من حسابك"), ("Your company is verified. Your approved jobs can now appear on the board.", "تم التحقق من شركتك. يمكن الآن ظهور وظائفك المعتمدة.")),
            EmployerVerification.Rejected => (("Verification was not approved", "لم تتم الموافقة على التحقق"), ($"We could not verify your company: {employer.VerificationNote}", $"تعذّر التحقق من شركتك: {employer.VerificationNote}")),
            _ => (("Your account is suspended", "تم تعليق حسابك"), ($"Your jobs are off the board: {employer.VerificationNote}", $"وظائفك غير ظاهرة: {employer.VerificationNote}"))
        };
        NotificationWriter.ToEmployer(unitOfWork, employer.Id, NotificationKinds.EmployerVerification, title, body, "/employer");
        await unitOfWork.SaveChangesAsync(ct);
        return (await AdminCareersSupport.EmployersAsync(unitOfWork, [employer], ct))[0];
    }
}

public class GetAdminJobsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdminJobsQuery, Result<PagedResult<AdminJobDto>>>
{
    public async Task<Result<PagedResult<AdminJobDto>>> Handle(GetAdminJobsQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var query = db.Set<Job>().AsNoTracking().AsQueryable();
        if (request.Statuses is { Count: > 0 })
        {
            var statuses = request.Statuses;
            query = query.Where(j => statuses.Contains(j.Status));
        }

        if (request.EmployerIds is { Count: > 0 })
        {
            var employers = request.EmployerIds;
            query = query.Where(j => employers.Contains(j.EmployerId));
        }

        if (request.CategoryIds is { Count: > 0 })
        {
            var categories = request.CategoryIds;
            query = query.Where(j => categories.Contains(j.CategoryId));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            var numeric = new string(term.Where(char.IsDigit).ToArray());
            var id = long.TryParse(numeric, out var parsed) ? parsed : -1;
            query = query.Where(j => j.Id == id || j.Title.ToLower().Contains(term) || j.Slug.Contains(term)
                || db.Set<Employer>().Any(e => e.Id == j.EmployerId && e.Name.ToLower().Contains(term))
                || db.Set<Location>().Any(l => l.Id == j.LocationId && (l.NameEn.ToLower().Contains(term) || l.NameAr.ToLower().Contains(term)))
                || db.Set<JobCategory>().Any(c => c.Id == j.CategoryId && (c.NameEn.ToLower().Contains(term) || c.NameAr.ToLower().Contains(term))));
        }

        var ascending = string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        query = request.SortField?.ToLowerInvariant() switch
        {
            "reference" => ascending ? query.OrderBy(j => j.Id) : query.OrderByDescending(j => j.Id),
            "title" => ascending ? query.OrderBy(j => j.Title) : query.OrderByDescending(j => j.Title),
            "status" => ascending ? query.OrderBy(j => j.Status) : query.OrderByDescending(j => j.Status),
            "createdat" => ascending ? query.OrderBy(j => j.CreatedAtUtc) : query.OrderByDescending(j => j.CreatedAtUtc),
            "applicationcount" => ascending
                ? query.OrderBy(j => db.Set<JobApplication>().Count(a => a.JobId == j.Id))
                : query.OrderByDescending(j => db.Set<JobApplication>().Count(a => a.JobId == j.Id)),
            "updatedat" => ascending ? query.OrderBy(j => j.LastModifiedUtc ?? j.CreatedAtUtc) : query.OrderByDescending(j => j.LastModifiedUtc ?? j.CreatedAtUtc),
            _ => query.OrderByDescending(j => j.LastModifiedUtc ?? j.CreatedAtUtc)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
        return new PagedResult<AdminJobDto>(await AdminCareersSupport.JobsAsync(unitOfWork, page.Items.ToList(), ct), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetAdminJobQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdminJobQuery, Result<AdminJobDto>>
{
    public async Task<Result<AdminJobDto>> Handle(GetAdminJobQuery request, CancellationToken ct)
    {
        var job = await unitOfWork.GetRepository<Job>().GetTableNoTracking().FirstOrDefaultAsync(j => j.Id == request.Id, ct);
        return job is null ? CareersErrors.JobNotFound : (await AdminCareersSupport.JobsAsync(unitOfWork, [job], ct))[0];
    }
}

public class ModerateJobCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ModerateJobCommand, Result<AdminJobDto>>
{
    public async Task<Result<AdminJobDto>> Handle(ModerateJobCommand request, CancellationToken ct)
    {
        var job = await unitOfWork.GetRepository<Job>().GetTableAsTracking().FirstOrDefaultAsync(j => j.Id == request.Id, ct);
        if (job is null)
        {
            return CareersErrors.JobNotFound;
        }

        if (request.Decision is not ("approve" or "reject"))
        {
            return CareersErrors.Field("decision", "Choose approve or reject.");
        }

        var employer = await unitOfWork.GetRepository<Employer>().GetTableNoTracking().FirstAsync(e => e.Id == job.EmployerId, ct);
        Result<Updated> moved = request.Decision == "approve" ? job.Approve(employer.IsVerified) : job.Reject(request.Note);
        if (moved.IsError)
        {
            return moved.Errors;
        }

        var approved = request.Decision == "approve";
        NotificationWriter.ToEmployer(unitOfWork, employer.Id, NotificationKinds.JobModerated,
            approved ? ("Your job is live", "وظيفتك منشورة") : ("Your job needs changes", "وظيفتك تحتاج إلى تعديل"),
            approved ? ($"\"{job.Title}\" was approved and is on the board.", $"تمت الموافقة على \"{job.Title}\" وهي الآن منشورة.")
                     : ($"\"{job.Title}\" was not approved: {job.ModerationNote}", $"لم تتم الموافقة على \"{job.Title}\": {job.ModerationNote}"),
            $"/employer/jobs/{job.Id}");
        await unitOfWork.SaveChangesAsync(ct);
        return (await AdminCareersSupport.JobsAsync(unitOfWork, [job], ct))[0];
    }
}

public class SetJobFeaturedCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<SetJobFeaturedCommand, Result<AdminJobDto>>
{
    public async Task<Result<AdminJobDto>> Handle(SetJobFeaturedCommand request, CancellationToken ct)
    {
        var job = await unitOfWork.GetRepository<Job>().GetTableAsTracking().FirstOrDefaultAsync(j => j.Id == request.Id, ct);
        if (job is null)
        {
            return CareersErrors.JobNotFound;
        }

        var set = job.SetFeatured(request.Featured);
        if (set.IsError)
        {
            return set.Errors;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return (await AdminCareersSupport.JobsAsync(unitOfWork, [job], ct))[0];
    }
}

public class CloseJobAsStaffCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CloseJobAsStaffCommand, Result<AdminJobDto>>
{
    public async Task<Result<AdminJobDto>> Handle(CloseJobAsStaffCommand request, CancellationToken ct)
    {
        var job = await unitOfWork.GetRepository<Job>().GetTableAsTracking().FirstOrDefaultAsync(j => j.Id == request.Id, ct);
        if (job is null)
        {
            return CareersErrors.JobNotFound;
        }

        var closed = job.Close();
        if (closed.IsError)
        {
            return closed.Errors;
        }

        NotificationWriter.ToEmployer(unitOfWork, job.EmployerId, NotificationKinds.JobModerated,
            ("JoFit closed your job", "أغلق JoFit وظيفتك"), ($"\"{job.Title}\" was closed by JoFit staff.", $"أغلق فريق JoFit وظيفة \"{job.Title}\"."), $"/employer/jobs/{job.Id}");
        await unitOfWork.SaveChangesAsync(ct);
        return (await AdminCareersSupport.JobsAsync(unitOfWork, [job], ct))[0];
    }
}

public class DeleteJobAsStaffCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteJobAsStaffCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteJobAsStaffCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<Job>();
        var job = await repository.GetTableAsTracking().FirstOrDefaultAsync(j => j.Id == request.Id, ct);
        if (job is null)
        {
            return CareersErrors.JobNotFound;
        }

        if (await unitOfWork.GetRepository<JobApplication>().GetTableNoTracking().AnyAsync(a => a.JobId == job.Id, ct))
        {
            return CareersErrors.HasApplications;
        }

        repository.DeleteAsync(job);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}

public class GetCareersPendingCountQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetCareersPendingCountQuery, Result<CareersPendingCountDto>>
{
    public async Task<Result<CareersPendingCountDto>> Handle(GetCareersPendingCountQuery request, CancellationToken ct)
        => new CareersPendingCountDto(
            await unitOfWork.GetRepository<Employer>().GetTableNoTracking().CountAsync(e => e.VerificationStatus == EmployerVerification.Pending, ct),
            await unitOfWork.GetRepository<Job>().GetTableNoTracking().CountAsync(j => j.Status == JobStates.Pending, ct));
}

public class GetAdminJobCategoriesQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdminJobCategoriesQuery, Result<PagedResult<AdminJobCategoryDto>>>
{
    public async Task<Result<PagedResult<AdminJobCategoryDto>>> Handle(GetAdminJobCategoriesQuery request, CancellationToken ct)
    {
        var all = await unitOfWork.GetRepository<JobCategory>().GetTableNoTracking().ToListAsync(ct);
        var dtos = await AdminCareersSupport.CategoriesAsync(unitOfWork, all, ct);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            dtos = dtos.Where(c => c.Name.En.Contains(term, StringComparison.OrdinalIgnoreCase) || c.Name.Ar.Contains(term, StringComparison.OrdinalIgnoreCase)
                || c.Slug.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var descending = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var arabic = string.Equals(request.Locale, "ar", StringComparison.OrdinalIgnoreCase);
        IEnumerable<AdminJobCategoryDto> sorted = request.SortField?.ToLowerInvariant() switch
        {
            "slug" => descending ? dtos.OrderByDescending(c => c.Slug) : dtos.OrderBy(c => c.Slug),
            "jobcount" => descending ? dtos.OrderByDescending(c => c.JobCount) : dtos.OrderBy(c => c.JobCount),
            _ => descending
                ? dtos.OrderByDescending(c => arabic ? c.Name.Ar : c.Name.En, StringComparer.CurrentCulture)
                : dtos.OrderBy(c => arabic ? c.Name.Ar : c.Name.En, StringComparer.CurrentCulture)
        };

        var list = sorted.ToList();
        var pageSize = Math.Clamp(request.PageSize, 1, PagingExtensions.AdminMaxPageSize);
        var totalPages = Math.Max(1, (int)Math.Ceiling(list.Count / (double)pageSize));
        var page = Math.Clamp(request.Page, 1, totalPages);
        return new PagedResult<AdminJobCategoryDto>(list.Skip((page - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, list.Count);
    }
}

public class GetJobCategoryOptionsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetJobCategoryOptionsQuery, Result<List<AdminJobCategoryDto>>>
{
    public async Task<Result<List<AdminJobCategoryDto>>> Handle(GetJobCategoryOptionsQuery request, CancellationToken ct)
    {
        var all = await unitOfWork.GetRepository<JobCategory>().GetTableNoTracking().ToListAsync(ct);
        var arabic = string.Equals(request.Locale, "ar", StringComparison.OrdinalIgnoreCase);
        return (await AdminCareersSupport.CategoriesAsync(unitOfWork, all, ct)).OrderBy(c => arabic ? c.Name.Ar : c.Name.En, StringComparer.CurrentCulture).ToList();
    }
}

public class SaveJobCategoryCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<SaveJobCategoryCommand, Result<AdminJobCategoryDto>>
{
    public async Task<Result<AdminJobCategoryDto>> Handle(SaveJobCategoryCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<JobCategory>();
        var values = new JobCategoryValues(request.Slug, request.NameEn, request.NameAr);
        JobCategory category;
        if (request.Id is long id)
        {
            var existing = await repository.GetTableAsTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
            if (existing is null)
            {
                return JobCategoryErrors.NotFound;
            }

            category = existing;
            var updated = category.Update(values);
            if (updated.IsError)
            {
                return updated.Errors;
            }
        }
        else
        {
            var created = JobCategory.Create(values);
            if (created.IsError)
            {
                return created.Errors;
            }

            category = created.Value;
            await repository.AddAsync(category, ct);
        }

        var slug = category.Slug;
        if (await repository.GetTableNoTracking().AnyAsync(c => c.Slug == slug && c.Id != category.Id, ct))
        {
            return JobCategoryErrors.SlugTaken;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return (await AdminCareersSupport.CategoriesAsync(unitOfWork, [category], ct))[0];
    }
}

public class DeleteJobCategoryCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteJobCategoryCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteJobCategoryCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<JobCategory>();
        var category = await repository.GetTableAsTracking().FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (category is null)
        {
            return JobCategoryErrors.NotFound;
        }

        var used = await unitOfWork.GetRepository<Job>().GetTableNoTracking().CountAsync(j => j.CategoryId == category.Id, ct);
        if (used > 0)
        {
            return CareersErrors.CategoryInUse(used);
        }

        repository.DeleteAsync(category);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}
