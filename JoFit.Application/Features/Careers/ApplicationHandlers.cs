using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Services;
using JoFit.Application.Features.Notifications;
using JoFit.Domain.Careers;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Customers;
using JoFit.Domain.Identity.Users;
using JoFit.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Careers;

public sealed record ApplicationInput(long JobId, long CvId, string? CoverLetter, int YearsExperience, string? Certifications, DateOnly? AvailableFrom, long? ExpectedSalaryAmount);

public sealed record UploadCvCommand(long UserId, byte[]? Bytes, string? FileName) : IRequest<Result<CvFileDto>>;

public sealed record SubmitApplicationCommand(long UserId, ApplicationInput Input) : IRequest<Result<CandidateApplicationDto>>;

public sealed record GetMyApplicationsQuery(long UserId) : IRequest<Result<List<CandidateApplicationDto>>>;

public sealed record GetMyApplicationByJobQuery(long UserId, long JobId) : IRequest<Result<CandidateApplicationDto>>;

public sealed record WithdrawMyApplicationCommand(long UserId, long ApplicationId, string? Reason) : IRequest<Result<CandidateApplicationDto>>;

public sealed record DownloadCvQuery(long ApplicationId, string Audience, long? UserId) : IRequest<Result<PrivateFile>>;

public sealed record GetEmployerApplicationsQuery(long UserId, int Page, int PageSize, IReadOnlyList<string>? Statuses, long? JobId, string? Search)
    : IRequest<Result<PagedResult<EmployerApplicationDto>>>;

public sealed record GetEmployerApplicationQuery(long UserId, long ApplicationId) : IRequest<Result<EmployerApplicationDto>>;

public sealed record SetEmployerApplicationStatusCommand(long UserId, long ApplicationId, string Status, string? Note) : IRequest<Result<EmployerApplicationDto>>;

public sealed record SetEmployerApplicationNoteCommand(long UserId, long ApplicationId, string? Note) : IRequest<Result<EmployerApplicationDto>>;

public sealed record GetAdminApplicationsQuery(
    int Page, int PageSize, string? Search, IReadOnlyList<string>? Statuses, IReadOnlyList<long>? JobIds, IReadOnlyList<long>? EmployerIds, long? CustomerId,
    string? SortField, string? SortDir) : IRequest<Result<PagedResult<AdminJobApplicationDto>>>;

public sealed record GetAdminApplicationQuery(long Id) : IRequest<Result<AdminJobApplicationDto>>;

internal static class ApplicationSupport
{
    public const string Pdf = "application/pdf";
    public const string Doc = "application/msword";
    public const string Docx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    /// <summary>The type by magic bytes: %PDF, D0 CF 11 E0 (old Word) or PK 03 04 (docx). Never by the name.</summary>
    public static string? DetectCv(byte[] b)
    {
        if (b.Length >= 4 && b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46) return Pdf;
        if (b.Length >= 4 && b[0] == 0xD0 && b[1] == 0xCF && b[2] == 0x11 && b[3] == 0xE0) return Doc;
        if (b.Length >= 4 && b[0] == 0x50 && b[1] == 0x4B && b[2] == 0x03 && b[3] == 0x04) return Docx;
        return null;
    }

    public static CvFileDto? Cv(CvFile? f) => f is null ? null : new CvFileDto(f.Id, f.FileName, f.MimeType, f.Bytes, f.CreatedAtUtc);

    public static IReadOnlyList<HistoryDto> History(JobApplication a) => a.History.Select(h => new HistoryDto(h.Status, h.At, h.By)).ToList();

    public static async Task<Dictionary<long, (Job Job, Employer Employer)>> JobsAsync(IUnitOfWork uow, IEnumerable<long> jobIds, CancellationToken ct)
    {
        var db = uow.GetDbContext();
        var ids = jobIds.Distinct().ToList();
        var jobs = await db.Set<Job>().IgnoreQueryFilters().AsNoTracking().Where(j => ids.Contains(j.Id)).ToListAsync(ct);
        var employerIds = jobs.Select(j => j.EmployerId).Distinct().ToList();
        var employers = await db.Set<Employer>().IgnoreQueryFilters().AsNoTracking().Where(e => employerIds.Contains(e.Id)).ToDictionaryAsync(e => e.Id, ct);
        return jobs.ToDictionary(j => j.Id, j => (j, employers[j.EmployerId]));
    }

    public static async Task<Dictionary<long, CvFile>> CvsAsync(IUnitOfWork uow, IEnumerable<long> cvIds, CancellationToken ct)
    {
        var ids = cvIds.Distinct().ToList();
        return await uow.GetDbContext().Set<CvFile>().IgnoreQueryFilters().AsNoTracking().Where(c => ids.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
    }

    public static ApplicationJobRef JobRef((Job Job, Employer Employer) x) => new(x.Job.Id, x.Job.Reference, x.Job.Title, x.Job.Slug, x.Employer.Name);

    public static async Task<List<CandidateApplicationDto>> CandidateAsync(IUnitOfWork uow, IReadOnlyList<JobApplication> apps, CancellationToken ct)
    {
        var jobs = await JobsAsync(uow, apps.Select(a => a.JobId), ct);
        var cvs = await CvsAsync(uow, apps.Select(a => a.CvFileId), ct);
        // The candidate never sees the employer's private note.
        return apps.Select(a => new CandidateApplicationDto(
            a.Id, a.Reference, JobRef(jobs[a.JobId]), a.Status, a.StatusNote, a.WithdrawReason, History(a), ApplicationStatuses.CanWithdraw(a.Status),
            Cv(cvs.GetValueOrDefault(a.CvFileId)), a.CoverLetter, a.YearsExperience, a.Certifications, a.AvailableFrom?.ToString("yyyy-MM-dd"),
            a.ExpectedSalaryAmount is long s ? new MoneyDto(s) : null, a.SubmittedAtUtc)).ToList();
    }

    public static async Task<List<EmployerApplicationDto>> EmployerAsync(IUnitOfWork uow, IReadOnlyList<JobApplication> apps, CancellationToken ct)
    {
        var jobs = await JobsAsync(uow, apps.Select(a => a.JobId), ct);
        var cvs = await CvsAsync(uow, apps.Select(a => a.CvFileId), ct);
        return apps.Select(a => new EmployerApplicationDto(
            a.Id, a.Reference, JobRef(jobs[a.JobId]), new CandidateDto(a.CandidateName, a.CandidateEmail, a.CandidatePhone), a.Status,
            ApplicationStatuses.EmployerMoves(a.Status), a.IsNew, a.StatusNote, a.WithdrawReason, a.EmployerNote, History(a), Cv(cvs.GetValueOrDefault(a.CvFileId)),
            a.CoverLetter, a.YearsExperience, a.Certifications, a.AvailableFrom?.ToString("yyyy-MM-dd"), a.ExpectedSalaryAmount is long s ? new MoneyDto(s) : null,
            a.SubmittedAtUtc, a.SeenByEmployerAtUtc)).ToList();
    }

    public static async Task<List<AdminJobApplicationDto>> AdminAsync(IUnitOfWork uow, IReadOnlyList<JobApplication> apps, CancellationToken ct)
    {
        var jobs = await JobsAsync(uow, apps.Select(a => a.JobId), ct);
        var cvs = await CvsAsync(uow, apps.Select(a => a.CvFileId), ct);
        // Staff never see the employer's private note, and the CV is metadata only.
        return apps.Select(a => new AdminJobApplicationDto(
            a.Id, a.Reference, JobRef(jobs[a.JobId]), jobs[a.JobId].Job.EmployerId, a.CustomerId, new CandidateDto(a.CandidateName, a.CandidateEmail, a.CandidatePhone),
            a.Status, a.StatusNote, a.WithdrawReason, History(a), Cv(cvs.GetValueOrDefault(a.CvFileId)), a.CoverLetter, a.YearsExperience, a.Certifications,
            a.AvailableFrom?.ToString("yyyy-MM-dd"), a.ExpectedSalaryAmount is long s ? new MoneyDto(s) : null, a.SubmittedAtUtc)).ToList();
    }

    /// <summary>What the candidate is told after an employer's move.</summary>
    public static ((string En, string Ar) Title, (string En, string Ar) Body) MoveCopy(string status, string jobTitle, string employer) => status switch
    {
        ApplicationStatuses.Reviewing => (("Your application is being reviewed", "يتم النظر في طلبك"), ($"{employer} is reviewing your application for \"{jobTitle}\".", $"{employer} يراجع طلبك لوظيفة \"{jobTitle}\".")),
        ApplicationStatuses.Shortlisted => (("You are shortlisted", "تم إدراجك في القائمة المختصرة"), ($"{employer} shortlisted you for \"{jobTitle}\".", $"{employer} أدرجك في القائمة المختصرة لوظيفة \"{jobTitle}\".")),
        ApplicationStatuses.Interview => (("Interview stage", "مرحلة المقابلة"), ($"{employer} would like to interview you for \"{jobTitle}\".", $"{employer} يرغب بمقابلتك لوظيفة \"{jobTitle}\".")),
        ApplicationStatuses.Offered => (("You have an offer", "لديك عرض"), ($"{employer} is making you an offer for \"{jobTitle}\".", $"{employer} يقدّم لك عرضاً لوظيفة \"{jobTitle}\".")),
        ApplicationStatuses.Hired => (("You are hired", "تم توظيفك"), ($"Congratulations! {employer} hired you for \"{jobTitle}\".", $"مبروك! {employer} وظّفك في \"{jobTitle}\".")),
        ApplicationStatuses.Rejected => (("Application update", "تحديث على طلبك"), ($"{employer} is not taking your application for \"{jobTitle}\" further.", $"{employer} لن يتابع طلبك لوظيفة \"{jobTitle}\".")),
        _ => (("Application update", "تحديث على طلبك"), ($"Your application for \"{jobTitle}\" changed.", $"تغيّر طلبك لوظيفة \"{jobTitle}\"."))
    };
}

public class UploadCvCommandHandler(IUnitOfWork unitOfWork, IPrivateFileStorage storage) : IRequestHandler<UploadCvCommand, Result<CvFileDto>>
{
    private const int MaxBytes = 5 * 1024 * 1024;

    public async Task<Result<CvFileDto>> Handle(UploadCvCommand request, CancellationToken ct)
    {
        var customerId = await unitOfWork.GetDbContext().Set<Customer>().AsNoTracking().Where(c => c.UserId == request.UserId).Select(c => (long?)c.Id).FirstOrDefaultAsync(ct);
        if (customerId is null)
        {
            return Error.Forbidden("Careers.NotCustomer", "This area is for customers.");
        }

        if (request.Bytes is null || request.Bytes.Length == 0)
        {
            return CareersErrors.Field("cv", "Choose a file to upload.");
        }

        if (request.Bytes.LongLength > MaxBytes)
        {
            return Error.Create((int)ErrorKindsEnum.PayloadTooLarge, "Careers.CvTooLarge", "The CV is larger than 5 MB.").ForField("cv");
        }

        var mime = ApplicationSupport.DetectCv(request.Bytes);
        if (mime is null)
        {
            return CareersErrors.Field("cv", "Upload a PDF, DOC or DOCX file.");
        }

        var key = $"cv-{Guid.NewGuid():N}";
        await storage.SaveAsync(key, request.Bytes, ct);
        var name = string.IsNullOrWhiteSpace(request.FileName) ? "cv" : Path.GetFileName(request.FileName.Trim());
        var cv = CvFile.Create(customerId.Value, name, mime, request.Bytes.LongLength, key);
        await unitOfWork.GetRepository<CvFile>().AddAsync(cv, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return ApplicationSupport.Cv(cv)!;
    }
}

public class SubmitApplicationCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<SubmitApplicationCommand, Result<CandidateApplicationDto>>
{
    public async Task<Result<CandidateApplicationDto>> Handle(SubmitApplicationCommand request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var customer = await db.Set<Customer>().AsNoTracking().FirstOrDefaultAsync(c => c.UserId == request.UserId, ct);
        if (customer is null)
        {
            return Error.Forbidden("Careers.NotCustomer", "This area is for customers.");
        }

        var input = request.Input;
        var job = await unitOfWork.GetRepository<Job>().GetTableAsTracking().FirstOrDefaultAsync(j => j.Id == input.JobId, ct);
        var employer = job is null ? null : await db.Set<Employer>().AsNoTracking().FirstOrDefaultAsync(e => e.Id == job.EmployerId, ct);
        if (job is null || employer is null)
        {
            return CareersErrors.JobNotFound;
        }

        // The CV must be the caller's own.
        var cv = await db.Set<CvFile>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == input.CvId && c.CustomerId == customer.Id, ct);
        var draft = new ApplicationDraft(job.Id, input.CvId, input.CoverLetter ?? string.Empty, input.YearsExperience, input.Certifications, input.AvailableFrom, input.ExpectedSalaryAmount);
        var email = await db.Set<User>().AsNoTracking().Where(u => u.Id == customer.UserId).Select(u => u.Email).FirstAsync(ct);
        var submitted = JobApplication.Submit(customer.Id, customer.Name, email!, customer.Phone, draft, CareersSupport.Today);

        var errors = new List<Error>();
        if (submitted.IsError) errors.AddRange(submitted.Errors);
        if (cv is null) errors.Add(CareersErrors.Field("cv", "Choose one of your own CV files."));
        if (errors.Count > 0)
        {
            return errors;
        }

        // The job must still be taking applications (open, employer verified, not past its date, not filled).
        var hires = await db.Set<JobApplication>().CountAsync(a => a.JobId == job.Id && a.Status == ApplicationStatuses.Hired, ct);
        if (!job.IsPublic(employer.IsVerified, CareersSupport.Today) || hires >= job.Positions)
        {
            return CareersErrors.NotAcceptingApplications;
        }

        // A second application is refused unless the earlier one was withdrawn; an employer's decision stands.
        var live = ApplicationStatuses.NotWithdrawn;
        if (await db.Set<JobApplication>().AnyAsync(a => a.JobId == job.Id && a.CustomerId == customer.Id && live.Contains(a.Status), ct))
        {
            return CareersErrors.AlreadyApplied;
        }

        var application = submitted.Value;
        try
        {
            await unitOfWork.GetRepository<JobApplication>().AddAsync(application, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return CareersErrors.AlreadyApplied;
        }

        NotificationWriter.ToEmployer(unitOfWork, employer.Id, NotificationKinds.JobApplication,
            ("New application", "طلب توظيف جديد"),
            ($"{customer.Name} applied for \"{job.Title}\" ({application.Reference}).", $"{customer.Name} قدّم على وظيفة \"{job.Title}\" ({application.Reference})."),
            $"/employer/applications/{application.Id}");
        await unitOfWork.SaveChangesAsync(ct);
        return (await ApplicationSupport.CandidateAsync(unitOfWork, [application], ct))[0];
    }
}

public class GetMyApplicationsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetMyApplicationsQuery, Result<List<CandidateApplicationDto>>>
{
    public async Task<Result<List<CandidateApplicationDto>>> Handle(GetMyApplicationsQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var customerId = await db.Set<Customer>().AsNoTracking().Where(c => c.UserId == request.UserId).Select(c => (long?)c.Id).FirstOrDefaultAsync(ct);
        var rows = await db.Set<JobApplication>().AsNoTracking().Where(a => a.CustomerId == customerId).OrderByDescending(a => a.SubmittedAtUtc).ToListAsync(ct);
        return await ApplicationSupport.CandidateAsync(unitOfWork, rows, ct);
    }
}

public class GetMyApplicationByJobQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetMyApplicationByJobQuery, Result<CandidateApplicationDto>>
{
    public async Task<Result<CandidateApplicationDto>> Handle(GetMyApplicationByJobQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var customerId = await db.Set<Customer>().AsNoTracking().Where(c => c.UserId == request.UserId).Select(c => (long?)c.Id).FirstOrDefaultAsync(ct);

        // The latest application that was not withdrawn; not found when there is none.
        var live = ApplicationStatuses.NotWithdrawn;
        var row = await db.Set<JobApplication>().AsNoTracking()
            .Where(a => a.CustomerId == customerId && a.JobId == request.JobId && live.Contains(a.Status)).OrderByDescending(a => a.SubmittedAtUtc).FirstOrDefaultAsync(ct);
        return row is null ? CareersErrors.ApplicationNotFound : (await ApplicationSupport.CandidateAsync(unitOfWork, [row], ct))[0];
    }
}

public class WithdrawMyApplicationCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<WithdrawMyApplicationCommand, Result<CandidateApplicationDto>>
{
    public async Task<Result<CandidateApplicationDto>> Handle(WithdrawMyApplicationCommand request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var customerId = await db.Set<Customer>().AsNoTracking().Where(c => c.UserId == request.UserId).Select(c => (long?)c.Id).FirstOrDefaultAsync(ct);
        var application = await unitOfWork.GetRepository<JobApplication>().GetTableAsTracking().FirstOrDefaultAsync(a => a.Id == request.ApplicationId && a.CustomerId == customerId, ct);
        if (application is null)
        {
            return CareersErrors.ApplicationNotFound;
        }

        var withdrawn = application.Withdraw(request.Reason);
        if (withdrawn.IsError)
        {
            return withdrawn.Errors;
        }

        var jobs = await ApplicationSupport.JobsAsync(unitOfWork, [application.JobId], ct);
        var (job, employer) = jobs[application.JobId];
        NotificationWriter.ToEmployer(unitOfWork, employer.Id, NotificationKinds.JobApplication,
            ("Application withdrawn", "تم سحب طلب"),
            ($"{application.CandidateName} withdrew their application for \"{job.Title}\".", $"{application.CandidateName} سحب طلبه لوظيفة \"{job.Title}\"."),
            $"/employer/applications/{application.Id}");
        await unitOfWork.SaveChangesAsync(ct);
        return (await ApplicationSupport.CandidateAsync(unitOfWork, [application], ct))[0];
    }
}

public class DownloadCvQueryHandler(IUnitOfWork unitOfWork, IPrivateFileStorage storage) : IRequestHandler<DownloadCvQuery, Result<PrivateFile>>
{
    public async Task<Result<PrivateFile>> Handle(DownloadCvQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var application = await db.Set<JobApplication>().AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct);
        if (application is null)
        {
            return CareersErrors.ApplicationNotFound;
        }

        // A CV goes to exactly one audience: the candidate, the employer applied to, or staff. Anyone else gets not-found.
        var allowed = request.Audience switch
        {
            "candidate" => await db.Set<Customer>().AnyAsync(c => c.UserId == request.UserId && c.Id == application.CustomerId, ct),
            "employer" => await db.Set<Employer>().AnyAsync(e => e.UserId == request.UserId
                && db.Set<Job>().Any(j => j.Id == application.JobId && j.EmployerId == e.Id), ct),
            "staff" => true,
            _ => false
        };
        if (!allowed)
        {
            return CareersErrors.ApplicationNotFound;
        }

        var cv = await db.Set<CvFile>().IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(c => c.Id == application.CvFileId, ct);
        var stream = cv is null ? null : storage.Open(cv.StorageKey);
        return cv is null || stream is null ? CareersErrors.CvNotFound : new PrivateFile(stream, cv.MimeType, cv.FileName);
    }
}

public class GetEmployerApplicationsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetEmployerApplicationsQuery, Result<PagedResult<EmployerApplicationDto>>>
{
    public async Task<Result<PagedResult<EmployerApplicationDto>>> Handle(GetEmployerApplicationsQuery request, CancellationToken ct)
    {
        var employer = await EmployerSupport.OfUserAsync(unitOfWork, request.UserId, false, ct);
        if (employer is null)
        {
            return CareersErrors.EmployerNotFound;
        }

        var db = unitOfWork.GetDbContext();
        var query = db.Set<JobApplication>().AsNoTracking().Where(a => db.Set<Job>().Any(j => j.Id == a.JobId && j.EmployerId == employer.Id));
        if (request.JobId is long jobId) query = query.Where(a => a.JobId == jobId);
        if (request.Statuses is { Count: > 0 })
        {
            var statuses = request.Statuses;
            query = query.Where(a => statuses.Contains(a.Status));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            var numeric = new string(term.Where(char.IsDigit).ToArray());
            var id = long.TryParse(numeric, out var parsed) ? parsed : -1;
            query = query.Where(a => a.Id == id || a.CandidateName.ToLower().Contains(term) || a.CandidateEmail.ToLower().Contains(term));
        }

        var page = await query.OrderByDescending(a => a.SubmittedAtUtc).ThenByDescending(a => a.Id).ToPagedResultAsync(request.Page, request.PageSize, ct, PagingExtensions.PublicMaxPageSize);
        return new PagedResult<EmployerApplicationDto>(await ApplicationSupport.EmployerAsync(unitOfWork, page.Items.ToList(), ct), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetEmployerApplicationQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetEmployerApplicationQuery, Result<EmployerApplicationDto>>
{
    public async Task<Result<EmployerApplicationDto>> Handle(GetEmployerApplicationQuery request, CancellationToken ct)
    {
        var employer = await EmployerSupport.OfUserAsync(unitOfWork, request.UserId, false, ct);
        var db = unitOfWork.GetDbContext();

        // Another employer's application is not found, never forbidden.
        var application = employer is null ? null : await unitOfWork.GetRepository<JobApplication>().GetTableAsTracking()
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && db.Set<Job>().Any(j => j.Id == a.JobId && j.EmployerId == employer.Id), ct);
        if (application is null)
        {
            return CareersErrors.ApplicationNotFound;
        }

        // Opening it marks it seen, so it is no longer "new".
        application.MarkSeen();
        await unitOfWork.SaveChangesAsync(ct);
        return (await ApplicationSupport.EmployerAsync(unitOfWork, [application], ct))[0];
    }
}

public class SetEmployerApplicationStatusCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<SetEmployerApplicationStatusCommand, Result<EmployerApplicationDto>>
{
    public async Task<Result<EmployerApplicationDto>> Handle(SetEmployerApplicationStatusCommand request, CancellationToken ct)
    {
        var employer = await EmployerSupport.OfUserAsync(unitOfWork, request.UserId, false, ct);
        var db = unitOfWork.GetDbContext();
        var application = employer is null ? null : await unitOfWork.GetRepository<JobApplication>().GetTableAsTracking()
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && db.Set<Job>().Any(j => j.Id == a.JobId && j.EmployerId == employer.Id), ct);
        if (employer is null || application is null)
        {
            return CareersErrors.ApplicationNotFound;
        }

        var moved = application.EmployerMove(request.Status, request.Note);
        if (moved.IsError)
        {
            return moved.Errors;
        }

        application.MarkSeen();
        var job = await unitOfWork.GetRepository<Job>().GetTableAsTracking().FirstAsync(j => j.Id == application.JobId, ct);

        // When the hires fill the positions the job closes by itself.
        if (request.Status == ApplicationStatuses.Hired)
        {
            var hires = await db.Set<JobApplication>().CountAsync(a => a.JobId == job.Id && a.Status == ApplicationStatuses.Hired && a.Id != application.Id, ct) + 1;
            if (hires >= job.Positions && job.Status == JobStates.Open) job.Close();
        }

        var copy = ApplicationSupport.MoveCopy(request.Status, job.Title, employer.Name);
        NotificationWriter.ToCustomer(unitOfWork, application.CustomerId, NotificationKinds.ApplicationStatus, copy.Title, copy.Body, "/account/applications");
        await unitOfWork.SaveChangesAsync(ct);
        return (await ApplicationSupport.EmployerAsync(unitOfWork, [application], ct))[0];
    }
}

public class SetEmployerApplicationNoteCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<SetEmployerApplicationNoteCommand, Result<EmployerApplicationDto>>
{
    public async Task<Result<EmployerApplicationDto>> Handle(SetEmployerApplicationNoteCommand request, CancellationToken ct)
    {
        var employer = await EmployerSupport.OfUserAsync(unitOfWork, request.UserId, false, ct);
        var db = unitOfWork.GetDbContext();
        var application = employer is null ? null : await unitOfWork.GetRepository<JobApplication>().GetTableAsTracking()
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && db.Set<Job>().Any(j => j.Id == a.JobId && j.EmployerId == employer.Id), ct);
        if (application is null)
        {
            return CareersErrors.ApplicationNotFound;
        }

        var set = application.SetEmployerNote(request.Note);
        if (set.IsError)
        {
            return set.Errors;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return (await ApplicationSupport.EmployerAsync(unitOfWork, [application], ct))[0];
    }
}

public class GetAdminApplicationsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdminApplicationsQuery, Result<PagedResult<AdminJobApplicationDto>>>
{
    public async Task<Result<PagedResult<AdminJobApplicationDto>>> Handle(GetAdminApplicationsQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var query = db.Set<JobApplication>().AsNoTracking().AsQueryable();

        if (request.Statuses is { Count: > 0 })
        {
            var statuses = request.Statuses;
            query = query.Where(a => statuses.Contains(a.Status));
        }

        if (request.JobIds is { Count: > 0 })
        {
            var jobs = request.JobIds;
            query = query.Where(a => jobs.Contains(a.JobId));
        }

        if (request.EmployerIds is { Count: > 0 })
        {
            var employers = request.EmployerIds;
            query = query.Where(a => db.Set<Job>().Any(j => j.Id == a.JobId && employers.Contains(j.EmployerId)));
        }

        if (request.CustomerId is long customerId) query = query.Where(a => a.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            var numeric = new string(term.Where(char.IsDigit).ToArray());
            var id = long.TryParse(numeric, out var parsed) ? parsed : -1;
            query = query.Where(a => a.Id == id || a.CandidateName.ToLower().Contains(term) || a.CandidateEmail.ToLower().Contains(term) || (a.CandidatePhone != null && a.CandidatePhone.Contains(term))
                || db.Set<Job>().Any(j => j.Id == a.JobId && (j.Title.ToLower().Contains(term) || j.Id == id
                    || db.Set<Employer>().Any(e => e.Id == j.EmployerId && e.Name.ToLower().Contains(term)))));
        }

        var ascending = string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        query = request.SortField?.ToLowerInvariant() switch
        {
            "reference" => ascending ? query.OrderBy(a => a.Id) : query.OrderByDescending(a => a.Id),
            "status" => ascending ? query.OrderBy(a => a.Status) : query.OrderByDescending(a => a.Status),
            "updatedat" => ascending ? query.OrderBy(a => a.LastModifiedUtc ?? a.CreatedAtUtc) : query.OrderByDescending(a => a.LastModifiedUtc ?? a.CreatedAtUtc),
            _ => ascending ? query.OrderBy(a => a.SubmittedAtUtc) : query.OrderByDescending(a => a.SubmittedAtUtc)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
        return new PagedResult<AdminJobApplicationDto>(await ApplicationSupport.AdminAsync(unitOfWork, page.Items.ToList(), ct), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetAdminApplicationQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdminApplicationQuery, Result<AdminJobApplicationDto>>
{
    public async Task<Result<AdminJobApplicationDto>> Handle(GetAdminApplicationQuery request, CancellationToken ct)
    {
        var application = await unitOfWork.GetRepository<JobApplication>().GetTableNoTracking().FirstOrDefaultAsync(a => a.Id == request.Id, ct);
        return application is null ? CareersErrors.ApplicationNotFound : (await ApplicationSupport.AdminAsync(unitOfWork, [application], ct))[0];
    }
}
