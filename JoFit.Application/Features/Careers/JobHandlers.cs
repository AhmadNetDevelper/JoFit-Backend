using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Services;
using JoFit.Application.Common.Utilities;
using JoFit.Application.Features.Notifications;
using JoFit.Domain.Careers;
using JoFit.Domain.Common.Results;
using JoFit.Domain.JobCategories;
using JoFit.Domain.Locations;
using JoFit.Domain.Notifications;
using JoFit.Domain.Sports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Careers;

/// <summary>A job form as saved. CityId is the city's key (for example "amman").</summary>
public sealed record JobInput(JobDetailsInput Details, IReadOnlyList<long> SportIds, string CityId);

/// <summary>The form fields; the city id is resolved separately.</summary>
public sealed record JobDetailsInput(
    string Title, long CategoryId, string EmploymentType, string WorkMode, string ExperienceLevel, SalaryInput? Salary, bool ShowSalary,
    string Description, IReadOnlyList<string> Requirements, IReadOnlyList<string> Benefits, IReadOnlyList<string> Languages, int Positions, DateOnly? ClosesOn);

public sealed record CreateEmployerJobCommand(long UserId, JobInput Input, bool Submit) : IRequest<Result<EmployerJobDto>>;

public sealed record UpdateEmployerJobCommand(long UserId, long JobId, JobInput Input) : IRequest<Result<EmployerJobDto>>;

public sealed record EmployerJobActionCommand(long UserId, long JobId, string Action) : IRequest<Result<EmployerJobDto>>;

public sealed record DeleteEmployerJobCommand(long UserId, long JobId) : IRequest<Result<Deleted>>;

public sealed record GetEmployerJobsQuery(long UserId, int Page, int PageSize, IReadOnlyList<string>? Statuses, string? Search) : IRequest<Result<PagedResult<EmployerJobDto>>>;

public sealed record GetEmployerJobQuery(long UserId, long JobId) : IRequest<Result<EmployerJobDto>>;

internal static class JobSupport
{
    public static async Task<List<Error>> CheckReferencesAsync(IUnitOfWork uow, JobInput input, CancellationToken ct)
    {
        var errors = new List<Error>();
        var db = uow.GetDbContext();
        if (input.Details.CategoryId > 0 && !await db.Set<JobCategory>().AnyAsync(c => c.Id == input.Details.CategoryId, ct))
            errors.Add(CareersErrors.Field("categoryId", "That category does not exist."));

        var sportIds = input.SportIds.Distinct().ToList();
        if (sportIds.Count > 0 && await db.Set<Sport>().CountAsync(s => sportIds.Contains(s.Id), ct) != sportIds.Count)
            errors.Add(CareersErrors.Field("sportIds", "One of the sports does not exist."));
        return errors;
    }

    public static JobDetails ToDetails(JobInput input, long locationId)
    {
        var d = input.Details;
        return new JobDetails(
            d.Title, d.CategoryId, locationId, d.EmploymentType, d.WorkMode, d.ExperienceLevel, d.Salary, d.ShowSalary,
            d.Description, d.Requirements, d.Benefits, d.Languages, d.Positions, d.ClosesOn);
    }

    /// <summary>The slug is the latin title plus the employer's slug (90 characters at most), with -2, -3 on a clash.</summary>
    public static async Task<string> SlugAsync(IUnitOfWork uow, string title, string employerSlug, CancellationToken ct)
    {
        var baseTitle = Slugger.Slugify(title);
        var suffix = "-" + employerSlug;
        var room = Math.Max(1, 90 - suffix.Length);
        var core = baseTitle.Length == 0 ? "job" : baseTitle.Length > room ? baseTitle[..room].TrimEnd('-') : baseTitle;
        var baseSlug = core + suffix;
        var taken = (await uow.GetDbContext().Set<Job>().IgnoreQueryFilters().Select(j => j.Slug).ToListAsync(ct)).ToHashSet();
        return Slugger.Unique(baseSlug, taken.Contains);
    }

    public static async Task ReplaceSportsAsync(IUnitOfWork uow, long jobId, IReadOnlyList<long> sportIds, CancellationToken ct)
    {
        var db = uow.GetDbContext();
        db.RemoveRange(await db.Set<JobSportLink>().Where(l => l.JobId == jobId).ToListAsync(ct));
        db.AddRange(sportIds.Distinct().Select(id => new JobSportLink { JobId = jobId, SportId = id }));
    }

    public static async Task<List<EmployerJobDto>> EmployerDtosAsync(IUnitOfWork uow, Employer employer, IReadOnlyList<Job> jobs, CancellationToken ct)
    {
        var db = uow.GetDbContext();
        var ids = jobs.Select(j => j.Id).ToList();
        var locations = await CareersSupport.LocationsByIdAsync(uow, ct);
        var sports = await db.Set<JobSportLink>().AsNoTracking().Where(l => ids.Contains(l.JobId)).ToListAsync(ct);
        var apps = await db.Set<JobApplication>().AsNoTracking().Where(a => ids.Contains(a.JobId)).Select(a => new { a.JobId, a.Status, a.SeenByEmployerAtUtc }).ToListAsync(ct);
        var today = CareersSupport.Today;

        return jobs.Select(j => new EmployerJobDto(
            j.Id, j.Reference, j.Slug, j.Title, j.CategoryId, sports.Where(l => l.JobId == j.Id).Select(l => l.SportId).OrderBy(x => x).ToList(),
            locations.GetValueOrDefault(j.LocationId)?.Slug ?? string.Empty, j.EmploymentType, j.WorkMode, j.ExperienceLevel, CareersSupport.Salary(j), j.ShowSalary,
            j.Description, j.Requirements, j.Benefits, j.LanguageList, j.Positions, j.ClosesOn?.ToString("yyyy-MM-dd"), j.Status, j.ModerationNote, j.Featured,
            j.IsPublic(employer.IsVerified, today), apps.Count(a => a.JobId == j.Id),
            apps.Count(a => a.JobId == j.Id && a.Status == ApplicationStatuses.Submitted && a.SeenByEmployerAtUtc == null),
            j.PostedAtUtc, j.CreatedAtUtc, j.LastModifiedUtc ?? j.CreatedAtUtc)).ToList();
    }

    public static void NotifyStaffSubmitted(IUnitOfWork uow, Employer employer, Job job, bool resubmitted)
        => NotificationWriter.ToStaff(uow, NotificationKinds.JobSubmitted,
            (resubmitted ? "Job resubmitted" : "Job submitted", resubmitted ? "أعيد إرسال وظيفة" : "تم إرسال وظيفة"),
            ($"{employer.Name} sent \"{job.Title}\" for review ({job.Reference}).", $"{employer.Name} أرسل \"{job.Title}\" للمراجعة ({job.Reference})."),
            $"/careers/jobs/{job.Id}");
}

public class CreateEmployerJobCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateEmployerJobCommand, Result<EmployerJobDto>>
{
    public async Task<Result<EmployerJobDto>> Handle(CreateEmployerJobCommand request, CancellationToken ct)
    {
        var employer = await EmployerSupport.OfUserAsync(unitOfWork, request.UserId, false, ct);
        if (employer is null)
        {
            return CareersErrors.EmployerNotFound;
        }

        var errors = await JobSupport.CheckReferencesAsync(unitOfWork, request.Input, ct);
        var locations = await CareersSupport.LocationsBySlugAsync(unitOfWork, ct);
        if (!locations.TryGetValue(request.Input.CityId ?? string.Empty, out var location)) errors.Add(CareersErrors.Field("cityId", "Choose one of the cities in the list."));

        var slug = await JobSupport.SlugAsync(unitOfWork, request.Input.Details.Title, employer.Slug, ct);
        var created = Job.Create(employer.Id, slug, JobSupport.ToDetails(request.Input, location?.Id ?? 0), CareersSupport.Today);
        if (created.IsError) errors.AddRange(created.Errors);
        if (errors.Count > 0)
        {
            return errors;
        }

        var job = created.Value;
        if (request.Submit)
        {
            // A rejected or suspended company cannot submit.
            if (employer.VerificationStatus is EmployerVerification.Rejected or EmployerVerification.Suspended)
            {
                return CareersErrors.EmployerBlocked;
            }

            job.Submit();
        }

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await unitOfWork.GetRepository<Job>().AddAsync(job, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await JobSupport.ReplaceSportsAsync(unitOfWork, job.Id, request.Input.SportIds, ct);
            if (request.Submit) JobSupport.NotifyStaffSubmitted(unitOfWork, employer, job, resubmitted: false);
            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }

        return (await JobSupport.EmployerDtosAsync(unitOfWork, employer, [job], ct))[0];
    }
}

public class UpdateEmployerJobCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateEmployerJobCommand, Result<EmployerJobDto>>
{
    public async Task<Result<EmployerJobDto>> Handle(UpdateEmployerJobCommand request, CancellationToken ct)
    {
        var employer = await EmployerSupport.OfUserAsync(unitOfWork, request.UserId, false, ct);
        var job = employer is null ? null : await unitOfWork.GetRepository<Job>().GetTableAsTracking().FirstOrDefaultAsync(j => j.Id == request.JobId && j.EmployerId == employer.Id, ct);
        if (employer is null || job is null)
        {
            return CareersErrors.JobNotFound;
        }

        var errors = await JobSupport.CheckReferencesAsync(unitOfWork, request.Input, ct);
        var locations = await CareersSupport.LocationsBySlugAsync(unitOfWork, ct);
        if (!locations.TryGetValue(request.Input.CityId ?? string.Empty, out var location)) errors.Add(CareersErrors.Field("cityId", "Choose one of the cities in the list."));

        // The closing date was checked when it was set; an unchanged past date is not an error on an unrelated edit.
        var today = request.Input.Details.ClosesOn == job.ClosesOn && job.ClosesOn is not null ? DateOnly.MinValue : CareersSupport.Today;
        var applied = job.Apply(JobSupport.ToDetails(request.Input, location?.Id ?? 0), today);
        if (applied.IsError) errors.AddRange(applied.Errors);
        if (errors.Count > 0)
        {
            return errors;
        }

        // What the public reads was approved, so editing an open job sends it back to review.
        var resubmitted = job.NeedsReviewAfterEdit;
        if (resubmitted) job.SendBackToReview();

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await JobSupport.ReplaceSportsAsync(unitOfWork, job.Id, request.Input.SportIds, ct);
            if (resubmitted) JobSupport.NotifyStaffSubmitted(unitOfWork, employer, job, resubmitted: true);
            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }

        return (await JobSupport.EmployerDtosAsync(unitOfWork, employer, [job], ct))[0];
    }
}

public class EmployerJobActionCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<EmployerJobActionCommand, Result<EmployerJobDto>>
{
    public async Task<Result<EmployerJobDto>> Handle(EmployerJobActionCommand request, CancellationToken ct)
    {
        var employer = await EmployerSupport.OfUserAsync(unitOfWork, request.UserId, false, ct);
        var job = employer is null ? null : await unitOfWork.GetRepository<Job>().GetTableAsTracking().FirstOrDefaultAsync(j => j.Id == request.JobId && j.EmployerId == employer.Id, ct);
        if (employer is null || job is null)
        {
            return CareersErrors.JobNotFound;
        }

        var blocked = employer.VerificationStatus is EmployerVerification.Rejected or EmployerVerification.Suspended;
        Result<Updated> result;
        switch (request.Action)
        {
            case "submit":
                // The status check comes first (409), then the employer's state (403).
                result = job.Status is JobStates.Draft or JobStates.Rejected ? (blocked ? CareersErrors.EmployerBlocked : job.Submit()) : job.Submit();
                break;
            case "withdraw":
                result = job.Withdraw();
                break;
            case "close":
                result = job.Close();
                break;
            case "reopen":
                result = job.Status == JobStates.Closed && blocked ? CareersErrors.EmployerBlocked : job.Reopen(CareersSupport.Today);
                break;
            default:
                return CareersErrors.Field("action", "Unknown action.");
        }

        if (result.IsError)
        {
            return result.Errors;
        }

        if (request.Action is "submit" or "reopen")
        {
            JobSupport.NotifyStaffSubmitted(unitOfWork, employer, job, resubmitted: request.Action == "reopen");
        }

        await unitOfWork.SaveChangesAsync(ct);
        return (await JobSupport.EmployerDtosAsync(unitOfWork, employer, [job], ct))[0];
    }
}

public class DeleteEmployerJobCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteEmployerJobCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteEmployerJobCommand request, CancellationToken ct)
    {
        var employer = await EmployerSupport.OfUserAsync(unitOfWork, request.UserId, false, ct);
        var repository = unitOfWork.GetRepository<Job>();
        var job = employer is null ? null : await repository.GetTableAsTracking().FirstOrDefaultAsync(j => j.Id == request.JobId && j.EmployerId == employer.Id, ct);
        if (job is null)
        {
            return CareersErrors.JobNotFound;
        }

        // Only while nobody has applied; otherwise close it instead.
        if (await unitOfWork.GetRepository<JobApplication>().GetTableNoTracking().AnyAsync(a => a.JobId == job.Id, ct))
        {
            return CareersErrors.HasApplications;
        }

        repository.DeleteAsync(job);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}

public class GetEmployerJobsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetEmployerJobsQuery, Result<PagedResult<EmployerJobDto>>>
{
    public async Task<Result<PagedResult<EmployerJobDto>>> Handle(GetEmployerJobsQuery request, CancellationToken ct)
    {
        var employer = await EmployerSupport.OfUserAsync(unitOfWork, request.UserId, false, ct);
        if (employer is null)
        {
            return CareersErrors.EmployerNotFound;
        }

        var query = unitOfWork.GetRepository<Job>().GetTableNoTracking().Where(j => j.EmployerId == employer.Id);
        if (request.Statuses is { Count: > 0 })
        {
            var statuses = request.Statuses;
            query = query.Where(j => statuses.Contains(j.Status));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            var numeric = new string(term.Where(char.IsDigit).ToArray());
            var id = long.TryParse(numeric, out var parsed) ? parsed : -1;
            query = query.Where(j => j.Title.ToLower().Contains(term) || j.Id == id);
        }

        // The newest change first.
        var page = await query.OrderByDescending(j => j.LastModifiedUtc ?? j.CreatedAtUtc).ThenByDescending(j => j.Id)
            .ToPagedResultAsync(request.Page, request.PageSize, ct, PagingExtensions.PublicMaxPageSize);
        return new PagedResult<EmployerJobDto>(await JobSupport.EmployerDtosAsync(unitOfWork, employer, page.Items.ToList(), ct), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetEmployerJobQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetEmployerJobQuery, Result<EmployerJobDto>>
{
    public async Task<Result<EmployerJobDto>> Handle(GetEmployerJobQuery request, CancellationToken ct)
    {
        var employer = await EmployerSupport.OfUserAsync(unitOfWork, request.UserId, false, ct);
        var job = employer is null ? null : await unitOfWork.GetRepository<Job>().GetTableNoTracking().FirstOrDefaultAsync(j => j.Id == request.JobId && j.EmployerId == employer.Id, ct);
        return employer is null || job is null ? CareersErrors.JobNotFound : (await JobSupport.EmployerDtosAsync(unitOfWork, employer, [job], ct))[0];
    }
}
