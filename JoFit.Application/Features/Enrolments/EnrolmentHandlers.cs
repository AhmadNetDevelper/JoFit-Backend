using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Services;
using JoFit.Application.Features.Customers;
using JoFit.Application.Features.Notifications;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Courses;
using JoFit.Domain.Customers;
using JoFit.Domain.Enrolments;
using JoFit.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Enrolments;

public sealed record CourseRefDto(long Id, string Slug, string Title);

public sealed record AccountEnrolmentDto(
    long Id, string Reference, CourseRefDto Course, string? CourseHref, string Status, string? CohortStartsOn,
    MoneyDto AmountPaid, int ProgressPercent, DateTimeOffset EnrolledAt);

public sealed record AdminEnrolmentDto(
    long Id, string Reference, long CourseId, LocalizedText CourseTitle, long CustomerId, string? CustomerName, string Status,
    IReadOnlyList<string> NextStatuses, string? CohortStartsOn, MoneyDto AmountPaid, int ProgressPercent, string? Note, DateTimeOffset EnrolledAt);

public sealed record EnrolCommand(long UserId, long CourseId, string Language) : IRequest<Result<AccountEnrolmentDto>>;

public sealed record GetMyEnrolmentsQuery(long UserId, string Language) : IRequest<Result<List<AccountEnrolmentDto>>>;

public sealed record GetMyEnrolmentByCourseQuery(long UserId, long CourseId, string Language) : IRequest<Result<AccountEnrolmentDto>>;

public sealed record GetAdminEnrolmentsQuery(
    int Page, int PageSize, string? Search, IReadOnlyList<string>? Statuses, long? CourseId, long? CustomerId, string? SortField, string? SortDir)
    : IRequest<Result<PagedResult<AdminEnrolmentDto>>>;

public sealed record SetEnrolmentStatusCommand(long Id, string Status, string? Note) : IRequest<Result<AdminEnrolmentDto>>;

/// <summary>Seats taken on a course's current intake: live (pending or active) enrolments for that intake date.</summary>
public static class CourseSeats
{
    public static async Task<Dictionary<long, int>> TakenAsync(IUnitOfWork unitOfWork, IReadOnlyList<Course> courses, CancellationToken ct)
    {
        var ids = courses.Select(c => c.Id).ToList();
        var live = EnrolmentStatuses.Live;
        var rows = await unitOfWork.GetDbContext().Set<Enrolment>().AsNoTracking()
            .Where(e => ids.Contains(e.CourseId) && live.Contains(e.Status))
            .Select(e => new { e.CourseId, e.CohortStartsOn }).ToListAsync(ct);

        return courses.ToDictionary(c => c.Id, c => rows.Count(r => r.CourseId == c.Id && r.CohortStartsOn == c.IntakeStartsOn));
    }

    public static async Task<Dictionary<long, int>> EnrolmentCountsAsync(IUnitOfWork unitOfWork, IReadOnlyList<long> courseIds, CancellationToken ct)
        => await unitOfWork.GetDbContext().Set<Enrolment>().AsNoTracking()
            .Where(e => courseIds.Contains(e.CourseId))
            .GroupBy(e => e.CourseId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
}

internal static class EnrolmentMapper
{
    public static async Task<List<AccountEnrolmentDto>> AccountAsync(
        IUnitOfWork unitOfWork, ILocalizationService localization, string language, IReadOnlyList<Enrolment> rows, CancellationToken ct)
    {
        var courseIds = rows.Select(r => r.CourseId).Distinct().ToList();
        var courses = await unitOfWork.GetDbContext().Set<Course>().IgnoreQueryFilters().AsNoTracking().Where(c => courseIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);

        return rows.Select(e =>
        {
            var course = courses.GetValueOrDefault(e.CourseId);
            return new AccountEnrolmentDto(
                e.Id, e.Reference,
                new CourseRefDto(e.CourseId, course?.Slug ?? string.Empty, course is null ? string.Empty : localization.GetLocalizedName(course.NameAr, course.NameEn, language)),
                // A link only while the course is still listed.
                course is { IsPublished: true, Slug: not null } && !course.IsDeleted ? $"/education/{course.Slug}" : null,
                e.Status, e.CohortStartsOn?.ToString("yyyy-MM-dd"), new MoneyDto(e.AmountPaidAmount), e.ProgressPercent, e.EnrolledAtUtc);
        }).ToList();
    }

    public static async Task<List<AdminEnrolmentDto>> AdminAsync(IUnitOfWork unitOfWork, IReadOnlyList<Enrolment> rows, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var courseIds = rows.Select(r => r.CourseId).Distinct().ToList();
        var customerIds = rows.Select(r => r.CustomerId).Distinct().ToList();
        var courses = await db.Set<Course>().IgnoreQueryFilters().AsNoTracking().Where(c => courseIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
        var customers = await db.Set<Customer>().AsNoTracking().Where(c => customerIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        return rows.Select(e =>
        {
            var course = courses.GetValueOrDefault(e.CourseId);
            return new AdminEnrolmentDto(
                e.Id, e.Reference, e.CourseId, new LocalizedText(course?.NameEn ?? string.Empty, course?.NameAr ?? string.Empty),
                e.CustomerId, customers.GetValueOrDefault(e.CustomerId),
                e.Status, EnrolmentStatuses.All.Where(s => EnrolmentStatuses.CanMove(e.Status, s)).ToList(),
                e.CohortStartsOn?.ToString("yyyy-MM-dd"), new MoneyDto(e.AmountPaidAmount), e.ProgressPercent, e.Note, e.EnrolledAtUtc);
        }).ToList();
    }
}

public class EnrolCommandHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<EnrolCommand, Result<AccountEnrolmentDto>>
{
    public async Task<Result<AccountEnrolmentDto>> Handle(EnrolCommand request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var customer = await db.Set<Customer>().AsNoTracking().FirstOrDefaultAsync(c => c.UserId == request.UserId, ct);
        if (customer is null)
        {
            return CustomerErrors.NotFound;
        }

        // Only a published course can be enrolled in; anything else is simply not there.
        var course = await db.Set<Course>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CourseId && c.Status == CourseStatuses.Published, ct);
        if (course is null)
        {
            return CourseErrors.CourseNotFound;
        }

        var live = EnrolmentStatuses.Live;
        if (await db.Set<Enrolment>().AnyAsync(e => e.CourseId == course.Id && e.CustomerId == customer.Id && live.Contains(e.Status), ct))
        {
            return EnrolmentErrors.AlreadyEnrolled;
        }

        if (course.IntakeSeatsTotal is int total)
        {
            var taken = (await CourseSeats.TakenAsync(unitOfWork, [course], ct))[course.Id];
            if (taken >= total)
            {
                return EnrolmentErrors.Full;
            }

            // Staff can mark the intake soon or full regardless of seats; the server enforces it.
            var state = course.IntakeState(taken, DateOnly.FromDateTime(DateTime.UtcNow));
            if (state == IntakeStates.Full)
            {
                return EnrolmentErrors.Full;
            }

            if (state == IntakeStates.Soon)
            {
                return EnrolmentErrors.NotOpen;
            }
        }

        var enrolment = Enrolment.Reserve(course.Id, customer.Id, course.IntakeStartsOn);
        try
        {
            await unitOfWork.GetRepository<Enrolment>().AddAsync(enrolment, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // The unique index on customer + course settled a double click.
            return EnrolmentErrors.AlreadyEnrolled;
        }

        NotificationWriter.ToStaff(unitOfWork, NotificationKinds.EnrolmentRequested,
            ("Enrolment reserved", "تم حجز تسجيل"),
            ($"{customer.Name} reserved a seat on {course.NameEn} ({enrolment.Reference}).", $"{customer.Name} حجز مقعداً في {course.NameAr} ({enrolment.Reference})."),
            "/enrolments");
        await unitOfWork.SaveChangesAsync(ct);

        return (await EnrolmentMapper.AccountAsync(unitOfWork, localization, request.Language, [enrolment], ct))[0];
    }
}

public class GetMyEnrolmentsQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetMyEnrolmentsQuery, Result<List<AccountEnrolmentDto>>>
{
    public async Task<Result<List<AccountEnrolmentDto>>> Handle(GetMyEnrolmentsQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var customerId = await db.Set<Customer>().AsNoTracking().Where(c => c.UserId == request.UserId).Select(c => (long?)c.Id).FirstOrDefaultAsync(ct);
        var rows = await db.Set<Enrolment>().AsNoTracking().Where(e => e.CustomerId == customerId).OrderByDescending(e => e.EnrolledAtUtc).ToListAsync(ct);
        return await EnrolmentMapper.AccountAsync(unitOfWork, localization, request.Language, rows, ct);
    }
}

public class GetMyEnrolmentByCourseQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetMyEnrolmentByCourseQuery, Result<AccountEnrolmentDto>>
{
    public async Task<Result<AccountEnrolmentDto>> Handle(GetMyEnrolmentByCourseQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var customerId = await db.Set<Customer>().AsNoTracking().Where(c => c.UserId == request.UserId).Select(c => (long?)c.Id).FirstOrDefaultAsync(ct);

        // The latest enrolment in this course; not found when there is none.
        var row = await db.Set<Enrolment>().AsNoTracking()
            .Where(e => e.CustomerId == customerId && e.CourseId == request.CourseId).OrderByDescending(e => e.EnrolledAtUtc).FirstOrDefaultAsync(ct);
        return row is null ? EnrolmentErrors.NotFound : (await EnrolmentMapper.AccountAsync(unitOfWork, localization, request.Language, [row], ct))[0];
    }
}

public class GetAdminEnrolmentsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdminEnrolmentsQuery, Result<PagedResult<AdminEnrolmentDto>>>
{
    public async Task<Result<PagedResult<AdminEnrolmentDto>>> Handle(GetAdminEnrolmentsQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var query = db.Set<Enrolment>().AsNoTracking().AsQueryable();

        if (request.Statuses is { Count: > 0 })
        {
            var statuses = request.Statuses;
            query = query.Where(e => statuses.Contains(e.Status));
        }

        if (request.CourseId is long courseId) query = query.Where(e => e.CourseId == courseId);
        if (request.CustomerId is long customerId) query = query.Where(e => e.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            var numeric = new string(term.Where(char.IsDigit).ToArray());
            var id = long.TryParse(numeric, out var parsed) ? parsed : -1;
            query = query.Where(e => e.Id == id
                || db.Set<Customer>().Any(c => c.Id == e.CustomerId && c.Name.ToLower().Contains(term))
                || db.Set<Course>().Any(c => c.Id == e.CourseId && (c.NameEn.ToLower().Contains(term) || c.NameAr.ToLower().Contains(term))));
        }

        var ascending = string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        query = request.SortField?.ToLowerInvariant() switch
        {
            "reference" => ascending ? query.OrderBy(e => e.Id) : query.OrderByDescending(e => e.Id),
            "cohortstartson" => ascending ? query.OrderBy(e => e.CohortStartsOn) : query.OrderByDescending(e => e.CohortStartsOn),
            "status" => ascending ? query.OrderBy(e => e.Status) : query.OrderByDescending(e => e.Status),
            _ => ascending ? query.OrderBy(e => e.EnrolledAtUtc) : query.OrderByDescending(e => e.EnrolledAtUtc)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
        return new PagedResult<AdminEnrolmentDto>(await EnrolmentMapper.AdminAsync(unitOfWork, page.Items.ToList(), ct), page.Page, page.PageSize, page.TotalCount);
    }
}

public class SetEnrolmentStatusCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<SetEnrolmentStatusCommand, Result<AdminEnrolmentDto>>
{
    public async Task<Result<AdminEnrolmentDto>> Handle(SetEnrolmentStatusCommand request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var enrolment = await unitOfWork.GetRepository<Enrolment>().GetTableAsTracking().FirstOrDefaultAsync(e => e.Id == request.Id, ct);
        if (enrolment is null)
        {
            return EnrolmentErrors.NotFound;
        }

        var course = await db.Set<Course>().IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(c => c.Id == enrolment.CourseId, ct);
        var moved = enrolment.MoveTo(request.Status, request.Note, course?.PriceAmount ?? 0);
        if (moved.IsError)
        {
            return moved.Errors;
        }

        var word = request.Status switch
        {
            EnrolmentStatuses.Active => ("confirmed", "تم تأكيده"),
            EnrolmentStatuses.Completed => ("completed", "مكتمل"),
            EnrolmentStatuses.Cancelled => ("cancelled", "ملغي"),
            _ => (request.Status, request.Status)
        };
        NotificationWriter.ToCustomer(unitOfWork, enrolment.CustomerId, NotificationKinds.EnrolmentStatus,
            ($"Your enrolment is {word.Item1}", $"تسجيلك {word.Item2}"),
            ($"Enrolment {enrolment.Reference} is now {word.Item1}.", $"أصبح التسجيل {enrolment.Reference} {word.Item2}."),
            "/account/courses");
        await unitOfWork.SaveChangesAsync(ct);
        return (await EnrolmentMapper.AdminAsync(unitOfWork, [enrolment], ct))[0];
    }
}
