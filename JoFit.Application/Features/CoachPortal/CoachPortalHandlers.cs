using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Utilities;
using JoFit.Application.Features.Coaches;
using JoFit.Application.Features.Enrolments;
using JoFit.Application.Features.Notifications;
using JoFit.Domain.Coaches;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Courses;
using JoFit.Domain.Enrolments;
using JoFit.Domain.Reviews;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.CoachPortal;

public sealed record CoachPortalSessionDto(
    long Id, string Reference, CoachBookingContactDto Client, string PackageName, string? Goal, string StartsAt, string EndsAt, string Status,
    MoneyDto Total, string? Note, IReadOnlyList<string> Next);

public sealed record CoachPortalCourseDto(long Id, string Slug, string Title, string Status, NextIntakeRefDto? NextIntake, int EnrolmentCount);

public sealed record NextIntakeRefDto(string StartsOn, int SeatsTotal);

public sealed record CoachPortalReviewDto(long Id, int Rating, string? Title, string Body, string AuthorName, DateTimeOffset CreatedAt);

public sealed record CoachPortalPackageDto(long Id, string Name, int DurationMinutes, int SessionCount, string Mode, MoneyDto Price, MoneyDto? CompareAtPrice);

public sealed record CoachDashboardCounts(int PendingSessions, int UpcomingSessions, int CompletedSessions, int Courses, int Reviews);

public sealed record CoachDashboardDto(long CoachId, string Name, string VerificationStatus, string? VerificationNote, CoachDashboardCounts Counts,
    CoachPortalSessionDto? NextSession, IReadOnlyList<CoachPortalSessionDto> PendingSessions);

public sealed record GetCoachDashboardQuery(long UserId, string Language) : IRequest<Result<CoachDashboardDto>>;

public sealed record GetCoachSessionsQuery(long UserId) : IRequest<Result<List<CoachPortalSessionDto>>>;

public sealed record SetCoachSessionStatusCommand(long UserId, long SessionId, string? Status, string? Note) : IRequest<Result<Deleted>>;

public sealed record GetCoachCoursesQuery(long UserId, string Language) : IRequest<Result<List<CoachPortalCourseDto>>>;

public sealed record GetCoachReviewsQuery(long UserId) : IRequest<Result<List<CoachPortalReviewDto>>>;

public sealed record GetCoachPackagesQuery(long UserId, string Language) : IRequest<Result<List<CoachPortalPackageDto>>>;

internal static class CoachPortalSupport
{
    /// <summary>The coach this signed-in user is. Always taken from the token, never from the request.</summary>
    public static async Task<Coach?> CoachOfAsync(IUnitOfWork unitOfWork, long userId, CancellationToken ct)
        => await unitOfWork.GetDbContext().Set<Coach>().AsNoTracking().Include(c => c.Packages).FirstOrDefaultAsync(c => c.UserId == userId, ct);

    /// <summary>Moves the coach may make now. Completing is allowed only after the session has started.</summary>
    public static IReadOnlyList<string> NextMoves(CoachBooking b)
        => CoachBookingStatuses.All
            .Where(s => CoachBookingStatuses.CanMove(b.Status, s) && !(s == CoachBookingStatuses.Completed && b.StartsAt > DateTimeOffset.UtcNow))
            .ToList();

    public static async Task<List<CoachPortalSessionDto>> SessionsAsync(IUnitOfWork unitOfWork, IReadOnlyList<CoachBooking> bookings, CancellationToken ct)
    {
        var packageIds = bookings.Select(b => b.PackageId).Distinct().ToList();
        var packages = await unitOfWork.GetDbContext().Set<CoachPackage>().IgnoreQueryFilters().AsNoTracking()
            .Where(p => packageIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.NameEn, ct);

        return bookings.Select(b => new CoachPortalSessionDto(
            b.Id, b.Reference, new CoachBookingContactDto(b.ContactName, b.ContactEmail, b.ContactPhone), packages.GetValueOrDefault(b.PackageId) ?? string.Empty, b.Goal,
            AmmanTime.ToAmman(b.StartsAt).ToString("yyyy-MM-dd'T'HH:mm:sszzz"), AmmanTime.ToAmman(b.EndsAt).ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
            b.Status, new MoneyDto(b.TotalAmount), b.Note, NextMoves(b))).ToList();
    }
}

public class GetCoachDashboardQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetCoachDashboardQuery, Result<CoachDashboardDto>>
{
    public async Task<Result<CoachDashboardDto>> Handle(GetCoachDashboardQuery request, CancellationToken ct)
    {
        var coach = await CoachPortalSupport.CoachOfAsync(unitOfWork, request.UserId, ct);
        if (coach is null)
        {
            return CoachErrors.NotFound;
        }

        var db = unitOfWork.GetDbContext();
        var mine = await db.Set<CoachBooking>().AsNoTracking().Where(b => b.CoachId == coach.Id).ToListAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var pending = mine.Where(b => b.Status == CoachBookingStatuses.Pending).OrderBy(b => b.StartsAt).ToList();
        var upcoming = mine.Where(b => b.Status is CoachBookingStatuses.Pending or CoachBookingStatuses.Confirmed && b.StartsAt > now).OrderBy(b => b.StartsAt).ToList();
        var courses = await db.Set<CourseInstructorLink>().CountAsync(l => l.CoachId == coach.Id, ct);
        var reviews = await db.Set<Review>().CountAsync(r => r.TargetType == ReviewTargets.Coach && r.TargetId == coach.Id && r.ModerationStatus == ReviewModeration.Approved, ct);

        var next = upcoming.Count == 0 ? null : (await CoachPortalSupport.SessionsAsync(unitOfWork, [upcoming[0]], ct))[0];
        var pendingDtos = await CoachPortalSupport.SessionsAsync(unitOfWork, pending.Take(5).ToList(), ct);
        return new CoachDashboardDto(
            coach.Id, coach.NameEn, coach.VerificationStatus, coach.VerificationNote,
            new CoachDashboardCounts(pending.Count, upcoming.Count, mine.Count(b => b.Status == CoachBookingStatuses.Completed), courses, reviews),
            next, pendingDtos);
    }
}

public class GetCoachSessionsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetCoachSessionsQuery, Result<List<CoachPortalSessionDto>>>
{
    public async Task<Result<List<CoachPortalSessionDto>>> Handle(GetCoachSessionsQuery request, CancellationToken ct)
    {
        var coach = await CoachPortalSupport.CoachOfAsync(unitOfWork, request.UserId, ct);
        if (coach is null)
        {
            return CoachErrors.NotFound;
        }

        var bookings = await unitOfWork.GetRepository<CoachBooking>().GetTableNoTracking().Where(b => b.CoachId == coach.Id).OrderByDescending(b => b.StartsAt).ToListAsync(ct);
        return await CoachPortalSupport.SessionsAsync(unitOfWork, bookings, ct);
    }
}

public class SetCoachSessionStatusCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<SetCoachSessionStatusCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(SetCoachSessionStatusCommand request, CancellationToken ct)
    {
        var coach = await CoachPortalSupport.CoachOfAsync(unitOfWork, request.UserId, ct);
        if (coach is null)
        {
            return CoachErrors.NotFound;
        }

        // Only the coach's own sessions; someone else's is simply not found.
        var booking = await unitOfWork.GetRepository<CoachBooking>().GetTableAsTracking().FirstOrDefaultAsync(b => b.Id == request.SessionId && b.CoachId == coach.Id, ct);
        if (booking is null)
        {
            return CoachErrors.BookingNotFound;
        }

        var status = request.Status ?? string.Empty;
        if (status == CoachBookingStatuses.Completed && booking.StartsAt > DateTimeOffset.UtcNow
            && CoachBookingStatuses.CanMove(booking.Status, status))
        {
            return Error.Conflict("Coach.NotStarted", "A session can be marked completed only after it starts.");
        }

        var moved = booking.MoveTo(status, request.Note);
        if (moved.IsError)
        {
            return moved.Errors;
        }

        // The customer is told; staff are not (the coach made this change).
        NotificationEvents.SessionStatusMoved(unitOfWork, booking.CustomerId, booking.CoachId, booking.Reference, status, byStaff: false);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}

public class GetCoachCoursesQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetCoachCoursesQuery, Result<List<CoachPortalCourseDto>>>
{
    public async Task<Result<List<CoachPortalCourseDto>>> Handle(GetCoachCoursesQuery request, CancellationToken ct)
    {
        var coach = await CoachPortalSupport.CoachOfAsync(unitOfWork, request.UserId, ct);
        if (coach is null)
        {
            return CoachErrors.NotFound;
        }

        var db = unitOfWork.GetDbContext();
        var courseIds = await db.Set<CourseInstructorLink>().Where(l => l.CoachId == coach.Id).Select(l => l.CourseId).ToListAsync(ct);
        var courses = await db.Set<Course>().AsNoTracking().Where(c => courseIds.Contains(c.Id) && c.Slug != null).OrderBy(c => c.NameEn).ToListAsync(ct);
        var counts = await CourseSeats.EnrolmentCountsAsync(unitOfWork, courseIds, ct);

        return courses.Select(c => new CoachPortalCourseDto(
            c.Id, c.Slug!, localization.GetLocalizedName(c.NameAr, c.NameEn, request.Language), c.Status,
            c.IntakeStartsOn is DateOnly d && c.IntakeSeatsTotal is int t ? new NextIntakeRefDto(d.ToString("yyyy-MM-dd"), t) : null,
            counts.GetValueOrDefault(c.Id))).ToList();
    }
}

public class GetCoachReviewsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetCoachReviewsQuery, Result<List<CoachPortalReviewDto>>>
{
    public async Task<Result<List<CoachPortalReviewDto>>> Handle(GetCoachReviewsQuery request, CancellationToken ct)
    {
        var coach = await CoachPortalSupport.CoachOfAsync(unitOfWork, request.UserId, ct);
        if (coach is null)
        {
            return CoachErrors.NotFound;
        }

        // Only approved reviews reach the coach.
        var rows = await unitOfWork.GetRepository<Review>().GetTableNoTracking()
            .Where(r => r.TargetType == ReviewTargets.Coach && r.TargetId == coach.Id && r.ModerationStatus == ReviewModeration.Approved)
            .OrderByDescending(r => r.SubmittedAtUtc).ToListAsync(ct);
        var customerIds = rows.Select(r => r.CustomerId).Distinct().ToList();
        var names = await unitOfWork.GetDbContext().Set<JoFit.Domain.Customers.Customer>().AsNoTracking()
            .Where(c => customerIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        string Author(long id)
        {
            var parts = (names.GetValueOrDefault(id) ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch { 0 => "Customer", 1 => parts[0], _ => $"{parts[0]} {parts[^1][0]}." };
        }

        return rows.Select(r => new CoachPortalReviewDto(r.Id, r.Rating, r.Title, r.Body, Author(r.CustomerId), r.SubmittedAtUtc)).ToList();
    }
}

public class GetCoachPackagesQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetCoachPackagesQuery, Result<List<CoachPortalPackageDto>>>
{
    public async Task<Result<List<CoachPortalPackageDto>>> Handle(GetCoachPackagesQuery request, CancellationToken ct)
    {
        var coach = await CoachPortalSupport.CoachOfAsync(unitOfWork, request.UserId, ct);
        if (coach is null)
        {
            return CoachErrors.NotFound;
        }

        return coach.Packages.OrderBy(p => p.Position).Select(p => new CoachPortalPackageDto(
            p.Id, localization.GetLocalizedName(p.NameAr, p.NameEn, request.Language), p.DurationMinutes, p.SessionCount, p.Mode,
            new MoneyDto(p.PriceAmount), p.CompareAtAmount is long c ? new MoneyDto(c) : null)).ToList();
    }
}
