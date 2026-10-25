using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Services;
using JoFit.Application.Common.Utilities;
using JoFit.Application.Features.Courses.Catalogue;
using JoFit.Domain.CoachSpecialisations;
using JoFit.Domain.Coaches;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Customers;
using JoFit.Domain.Identity.Users;
using JoFit.Domain.Locations;
using JoFit.Domain.Sports;
using JoFit.Domain.UserCertificates;
using JoFit.Domain.UserEducations;
using JoFit.Domain.UserExperiences;
using JoFit.Domain.UserSkills;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Coaches;

/// <summary>
/// A coach form as saved. Avatar and Gallery: null keeps what is stored, an empty list clears it, a list replaces it.
/// </summary>
public sealed record CoachInput(
    CoachDetails Details,
    IReadOnlyList<long> SportIds,
    IReadOnlyList<long> SpecialisationIds,
    IReadOnlyList<GalleryItem>? Avatar,
    IReadOnlyList<GalleryItem>? Gallery,
    string? LoginPassword);

public sealed record CreateCoachProfileCommand(CoachInput Input) : IRequest<Result<AdminCoachDto>>;

public sealed record UpdateCoachProfileCommand(long Id, CoachInput Input) : IRequest<Result<AdminCoachDto>>;

public sealed record DeleteCoachProfileCommand(long Id) : IRequest<Result<Deleted>>;

public sealed record DecideCoachVerificationCommand(long Id, string Status, string? Note) : IRequest<Result<AdminCoachDto>>;

public sealed record SetCoachCertificationVerifiedCommand(long CoachId, long CertificationId, bool IsVerified) : IRequest<Result<AdminCoachDto>>;

public sealed record CoachBookingContactInput(string? Name, string? Email, string? Phone);

public sealed record CreateCoachBookingCommand(
    long CoachId, long PackageId, string? SlotId, CoachBookingContactInput Contact, string? Goal, long? CustomerUserId)
    : IRequest<Result<CoachBookingDto>>;

public sealed record SetCoachBookingStatusCommand(long Id, string Status, string? Note) : IRequest<Result<CoachBookingDto>>;

/// <summary>Weekly schedule to slots. Slot ids are opaque to the website: slot-coachId-yyyyMMdd-HH.</summary>
public static class CoachSlots
{
    public const int MaxWindowDays = 60;

    public static string IdOf(long coachId, DateOnly date, int hour) => $"slot-{coachId}-{date:yyyyMMdd}-{hour:D2}";

    /// <summary>Reads a slot id; null when it is not one.</summary>
    public static (long CoachId, DateOnly Date, int Hour)? Parse(string? id)
    {
        var parts = (id ?? string.Empty).Split('-');
        if (parts.Length != 4 || parts[0] != "slot" || !long.TryParse(parts[1], out var coachId)
            || !DateOnly.TryParseExact(parts[2], "yyyyMMdd", out var date) || !int.TryParse(parts[3], out var hour))
        {
            return null;
        }

        return (coachId, date, hour);
    }

    /// <summary>Whether the coach offers this hour on this date (working day, inside the day, still ahead).</summary>
    public static bool IsOffered(Coach coach, DateOnly date, int hour)
        => hour >= coach.DayStartHour && hour < coach.DayEndHour
           && coach.WorkingDayList.Contains(date.DayOfWeek)
           && AmmanTime.At(date, hour) > DateTimeOffset.UtcNow;

    public static IEnumerable<(DateOnly Date, int Hour)> Offered(Coach coach, DateOnly from, DateOnly to)
    {
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            if (!coach.WorkingDayList.Contains(day.DayOfWeek))
            {
                continue;
            }

            for (var hour = coach.DayStartHour; hour < coach.DayEndHour; hour++)
            {
                if (AmmanTime.At(day, hour) > DateTimeOffset.UtcNow)
                {
                    yield return (day, hour);
                }
            }
        }
    }
}

internal static class CoachSaving
{
    public const string OwnerType = "coach";

    public static async Task<List<Error>> CheckReferencesAsync(IUnitOfWork unitOfWork, CoachInput input, long? selfId, CancellationToken ct)
    {
        var errors = new List<Error>();
        var db = unitOfWork.GetDbContext();

        if (input.Details.LocationId > 0 && !await db.Set<Location>().AnyAsync(l => l.Id == input.Details.LocationId, ct))
        {
            errors.Add(CoachErrors.LocationMissing);
        }

        var sportIds = input.SportIds.Distinct().ToList();
        if (sportIds.Count == 0 || await db.Set<Sport>().CountAsync(s => sportIds.Contains(s.Id), ct) != sportIds.Count)
        {
            errors.Add(CoachErrors.SportsMissing);
        }

        var specIds = input.SpecialisationIds.Distinct().ToList();
        if (specIds.Count > 0 && await db.Set<CoachSpecialisation>().CountAsync(s => specIds.Contains(s.Id), ct) != specIds.Count)
        {
            errors.Add(CoachErrors.SpecialisationsMissing);
        }

        var slug = input.Details.Slug?.Trim();
        if (!string.IsNullOrEmpty(slug) && await db.Set<Coach>().AnyAsync(c => c.Slug == slug && c.Id != selfId, ct))
        {
            errors.Add(CoachErrors.SlugTaken);
        }

        var email = input.Details.Email?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(email) && await db.Set<Coach>().AnyAsync(c => c.Email == email && c.Id != selfId, ct))
        {
            errors.Add(CoachErrors.EmailTaken);
        }

        return errors;
    }

    public static async Task ReplaceLinksAsync(IUnitOfWork unitOfWork, long coachId, CoachInput input, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        db.RemoveRange(await db.Set<CoachSportLink>().Where(l => l.CoachId == coachId).ToListAsync(ct));
        db.RemoveRange(await db.Set<CoachSpecialisationLink>().Where(l => l.CoachId == coachId).ToListAsync(ct));
        db.AddRange(input.SportIds.Distinct().Select(id => new CoachSportLink { CoachId = coachId, SportId = id }));
        db.AddRange(input.SpecialisationIds.Distinct().Select(id => new CoachSpecialisationLink { CoachId = coachId, CoachSpecialisationId = id }));
    }

    public static async Task<Result<Updated>> SaveMediaAsync(IUnitOfWork unitOfWork, IUrlService urlService, long coachId, CoachInput input, CancellationToken ct)
    {
        if (input.Avatar is not null)
        {
            var avatar = await GalleryService.SyncAsync(unitOfWork, urlService, OwnerType, coachId, "avatar", input.Avatar.Take(1).ToList(), ct);
            if (avatar.IsError) return avatar.Errors;
        }

        if (input.Gallery is not null)
        {
            var gallery = await GalleryService.SyncAsync(unitOfWork, urlService, OwnerType, coachId, "gallery", input.Gallery, ct);
            if (gallery.IsError) return gallery.Errors;
        }

        return Result.Updated;
    }

    public static async Task<AdminCoachDto> ToDtoAsync(IUnitOfWork unitOfWork, IUrlService urlService, long id, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var coach = await db.Set<Coach>().AsNoTracking().Include(c => c.Packages).Include(c => c.Certifications).FirstAsync(c => c.Id == id, ct);
        var sportIds = await db.Set<CoachSportLink>().AsNoTracking().Where(l => l.CoachId == id).Select(l => l.SportId).OrderBy(x => x).ToListAsync(ct);
        var specIds = await db.Set<CoachSpecialisationLink>().AsNoTracking().Where(l => l.CoachId == id).Select(l => l.CoachSpecialisationId).OrderBy(x => x).ToListAsync(ct);
        var avatar = await GalleryService.LoadAsync(unitOfWork, OwnerType, "avatar", [id], ct);
        var gallery = await GalleryService.LoadAsync(unitOfWork, OwnerType, "gallery", [id], ct);
        var completed = await db.Set<CoachBooking>().CountAsync(b => b.CoachId == id && b.Status == CoachBookingStatuses.Completed, ct);
        var dto = AdminCoachDto.From(coach, sportIds, specIds, avatar.GetValueOrDefault(id)?.FirstOrDefault(), gallery.GetValueOrDefault(id) ?? [], completed);
        if (coach.UserId is not long userId)
        {
            return dto;
        }

        var user = await db.Set<User>().AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        var educations = await db.Set<UserEducation>().AsNoTracking().Include(x => x.AcademicDegree).Include(x => x.UniversityMajor)
            .Where(x => x.UserId == userId).OrderBy(x => x.Id).ToListAsync(ct);
        var experiences = await db.Set<UserExperience>().AsNoTracking().Include(x => x.JobType)
            .Where(x => x.UserId == userId).OrderBy(x => x.Id).ToListAsync(ct);
        var certificates = await db.Set<UserCertificate>().AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.Id).ToListAsync(ct);
        var skills = await db.Set<UserSkill>().AsNoTracking().Include(x => x.Course).Where(x => x.UserId == userId).OrderBy(x => x.Id).ToListAsync(ct);

        return dto with
        {
            Account = user is null ? null : new CoachAccountDto(
                user.Id, user.UserName ?? string.Empty, user.FullName, user.PhoneNumber, user.BirthOfDate, (int)user.GenderId,
                user.CountryId, user.CityId, user.JobStatusId, user.IsActive, urlService.ToAbsoluteUrl(user.ProfileImageUrl)),
            Educations = educations.Select(x => new CoachEducationDto(
                x.Id, x.SchoolName, x.AcademicDegreeId, x.AcademicDegree is null ? null : new LocalizedText(x.AcademicDegree.NameEn, x.AcademicDegree.NameAr),
                x.UniversityMajorId, x.UniversityMajor is null ? null : new LocalizedText(x.UniversityMajor.NameEn, x.UniversityMajor.NameAr),
                x.StartDate, x.EndDate, x.IsStillEducation)).ToList(),
            Experiences = experiences.Select(x => new CoachExperienceDto(
                x.Id, x.Address, x.JobTypeId, x.JobType is null ? null : new LocalizedText(x.JobType.NameEn, x.JobType.NameAr),
                x.CompanyName, x.CompanyAddress, x.StartDate, x.EndDate, x.IsStillWorking)).ToList(),
            UserCertificates = certificates.Select(x => new CoachUserCertificateDto(
                x.Id, x.CertificateName, x.OrganizationName, x.IssuedDate, x.ExpiryDate, x.CredentialId, x.CredentialUrl, urlService.ToAbsoluteUrl(x.CertificateUrl))).ToList(),
            Skills = skills.Select(x => new CoachSkillDto(
                x.Id, x.CourseId, x.Course is null ? null : new LocalizedText(x.Course.NameEn, x.Course.NameAr), x.PercentageScore)).ToList()
        };
    }

    /// <summary>Matches packages by id (keeping bookings pointed at the right one), adds new ones and retires the rest.</summary>
    public static Result<Updated> SyncPackages(Coach coach, IReadOnlyList<CoachPackageInput> packages, IUnitOfWork unitOfWork)
    {
        var existing = coach.Packages.ToDictionary(p => p.Id);
        var errors = new List<Error>();
        var keep = new HashSet<long>();

        for (var i = 0; i < packages.Count; i++)
        {
            var input = packages[i];
            if (input.Id is long id)
            {
                if (!existing.TryGetValue(id, out var package))
                {
                    errors.Add(CoachErrors.Field($"packages.{i}.id", "That package does not belong to this coach."));
                    continue;
                }

                package.Apply(input, i);
                keep.Add(id);
            }
            else
            {
                coach.Packages.Add(CoachPackage.Create(input, i));
            }
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        // Packages dropped from the form are soft-deleted so past bookings still show their name.
        foreach (var gone in existing.Values.Where(p => !keep.Contains(p.Id)))
        {
            unitOfWork.GetRepository<CoachPackage>().DeleteAsync(gone);
        }

        return Result.Updated;
    }
}

public class CreateCoachProfileCommandHandler(IUnitOfWork unitOfWork, IUrlService urlService, IAccountService accounts)
    : IRequestHandler<CreateCoachProfileCommand, Result<AdminCoachDto>>
{
    public async Task<Result<AdminCoachDto>> Handle(CreateCoachProfileCommand request, CancellationToken ct)
    {
        var input = request.Input;
        var errors = await CoachSaving.CheckReferencesAsync(unitOfWork, input, null, ct);
        if (input.LoginPassword is { Length: > 0 and < 8 })
        {
            errors.Add(CoachErrors.Field("password", "Use at least 8 characters."));
        }

        var created = Coach.Create(input.Details);
        if (created.IsError) errors.AddRange(created.Errors);
        if (errors.Count > 0)
        {
            return errors;
        }

        var coach = created.Value;
        for (var i = 0; i < input.Details.Packages.Count; i++)
        {
            coach.Packages.Add(CoachPackage.Create(input.Details.Packages[i] with { Id = null }, i));
        }

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            if (!string.IsNullOrEmpty(input.LoginPassword))
            {
                var login = await accounts.CreateCoachLoginAsync(coach.Email, coach.NameEn, input.LoginPassword, ct);
                if (login.IsError)
                {
                    await unitOfWork.RollbackAsync(ct);
                    return login.Errors;
                }

                coach.LinkLogin(login.Value);
            }

            await unitOfWork.GetRepository<Coach>().AddAsync(coach, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await CoachSaving.ReplaceLinksAsync(unitOfWork, coach.Id, input, ct);
            var media = await CoachSaving.SaveMediaAsync(unitOfWork, urlService, coach.Id, input, ct);
            if (media.IsError)
            {
                await unitOfWork.RollbackAsync(ct);
                return media.Errors;
            }

            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }

        return await CoachSaving.ToDtoAsync(unitOfWork, urlService, coach.Id, ct);
    }
}

public class UpdateCoachProfileCommandHandler(IUnitOfWork unitOfWork, IUrlService urlService)
    : IRequestHandler<UpdateCoachProfileCommand, Result<AdminCoachDto>>
{
    public async Task<Result<AdminCoachDto>> Handle(UpdateCoachProfileCommand request, CancellationToken ct)
    {
        var input = request.Input;
        var coach = await unitOfWork.GetRepository<Coach>().GetTableAsTracking().Include(c => c.Packages).Include(c => c.Certifications)
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (coach is null)
        {
            return CoachErrors.NotFound;
        }

        var errors = await CoachSaving.CheckReferencesAsync(unitOfWork, input, coach.Id, ct);
        var applied = coach.Apply(input.Details);
        if (applied.IsError) errors.AddRange(applied.Errors);
        var packages = CoachSaving.SyncPackages(coach, input.Details.Packages, unitOfWork);
        if (packages.IsError) errors.AddRange(packages.Errors);
        if (errors.Count > 0)
        {
            return errors;
        }

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await CoachSaving.ReplaceLinksAsync(unitOfWork, coach.Id, input, ct);
            var media = await CoachSaving.SaveMediaAsync(unitOfWork, urlService, coach.Id, input, ct);
            if (media.IsError)
            {
                await unitOfWork.RollbackAsync(ct);
                return media.Errors;
            }

            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }

        return await CoachSaving.ToDtoAsync(unitOfWork, urlService, coach.Id, ct);
    }
}

public class DeleteCoachProfileCommandHandler(IUnitOfWork unitOfWork, IAccountService accounts)
    : IRequestHandler<DeleteCoachProfileCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteCoachProfileCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<Coach>();
        var coach = await repository.GetTableAsTracking().FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (coach is null)
        {
            return CoachErrors.NotFound;
        }

        // Completed sessions do not block; pending and confirmed ones do. Teaching a course will block too once instructors exist.
        var open = await unitOfWork.GetRepository<CoachBooking>().GetTableNoTracking()
            .CountAsync(b => b.CoachId == coach.Id && (b.Status == CoachBookingStatuses.Pending || b.Status == CoachBookingStatuses.Confirmed), ct);
        if (open > 0)
        {
            return CoachErrors.InUse(open);
        }

        var teaches = await unitOfWork.GetDbContext().Set<JoFit.Domain.Courses.CourseInstructorLink>().CountAsync(l => l.CoachId == coach.Id, ct);
        if (teaches > 0)
        {
            return Error.Conflict("Coach.TeachesCourses", $"This coach teaches {teaches} course(s). Remove them as an instructor first.");
        }

        if (coach.UserId is long loginId)
        {
            await accounts.DeactivateAsync(loginId, ct);
        }

        repository.DeleteAsync(coach);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}

public class DecideCoachVerificationCommandHandler(IUnitOfWork unitOfWork, IUrlService urlService)
    : IRequestHandler<DecideCoachVerificationCommand, Result<AdminCoachDto>>
{
    public async Task<Result<AdminCoachDto>> Handle(DecideCoachVerificationCommand request, CancellationToken ct)
    {
        var coach = await unitOfWork.GetRepository<Coach>().GetTableAsTracking().FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (coach is null)
        {
            return CoachErrors.NotFound;
        }

        var decided = coach.Decide(request.Status, request.Note);
        if (decided.IsError)
        {
            return decided.Errors;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return await CoachSaving.ToDtoAsync(unitOfWork, urlService, coach.Id, ct);
    }
}

public class SetCoachCertificationVerifiedCommandHandler(IUnitOfWork unitOfWork, IUrlService urlService)
    : IRequestHandler<SetCoachCertificationVerifiedCommand, Result<AdminCoachDto>>
{
    public async Task<Result<AdminCoachDto>> Handle(SetCoachCertificationVerifiedCommand request, CancellationToken ct)
    {
        var certification = await unitOfWork.GetRepository<CoachCertification>().GetTableAsTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CertificationId && c.CoachId == request.CoachId, ct);
        if (certification is null)
        {
            return CoachErrors.CertificationNotFound;
        }

        certification.SetVerified(request.IsVerified);
        await unitOfWork.SaveChangesAsync(ct);
        return await CoachSaving.ToDtoAsync(unitOfWork, urlService, request.CoachId, ct);
    }
}

public class CreateCoachBookingCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCoachBookingCommand, Result<CoachBookingDto>>
{
    public async Task<Result<CoachBookingDto>> Handle(CreateCoachBookingCommand request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();

        // Only approved coaches take bookings; anyone else is simply not there.
        var coach = await db.Set<Coach>().AsNoTracking().Include(c => c.Packages)
            .FirstOrDefaultAsync(c => c.Id == request.CoachId && c.VerificationStatus == CoachVerification.Approved, ct);
        if (coach is null)
        {
            return CoachErrors.NotFound;
        }

        var package = coach.Packages.FirstOrDefault(p => p.Id == request.PackageId);
        if (package is null)
        {
            return CoachErrors.PackageNotOffered;
        }

        var slot = CoachSlots.Parse(request.SlotId);
        if (slot is null || slot.Value.CoachId != coach.Id || !CoachSlots.IsOffered(coach, slot.Value.Date, slot.Value.Hour))
        {
            return CoachErrors.SlotNotFound;
        }

        // Stored and compared in UTC; shown back in Amman time.
        var startsAt = AmmanTime.At(slot.Value.Date, slot.Value.Hour).ToUniversalTime();
        var endsAt = startsAt.AddMinutes(package.DurationMinutes);

        // The slot must be free: no live booking that overlaps this session.
        var live = CoachBookingStatuses.Live;
        var taken = await db.Set<CoachBooking>().AnyAsync(b => b.CoachId == coach.Id && live.Contains(b.Status) && b.StartsAt < endsAt && b.EndsAt > startsAt, ct);
        if (taken)
        {
            return CoachErrors.SlotTaken;
        }

        // A signed-in customer's booking is filed on their account; a guest's has no customer.
        long? customerId = request.CustomerUserId is long userId
            ? await db.Set<Customer>().AsNoTracking().Where(c => c.UserId == userId).Select(c => (long?)c.Id).FirstOrDefaultAsync(ct)
            : null;

        var contact = request.Contact;
        var booking = CoachBooking.Create(
            coach.Id, package.Id, customerId,
            contact.Name?.Trim() ?? string.Empty, contact.Email?.Trim() ?? string.Empty,
            string.IsNullOrWhiteSpace(contact.Phone) ? null : contact.Phone.Trim(),
            string.IsNullOrWhiteSpace(request.Goal) ? null : request.Goal.Trim(),
            startsAt, endsAt, package.PriceAmount);

        try
        {
            await unitOfWork.GetRepository<CoachBooking>().AddAsync(booking, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // The unique index on coach + start time settled a race with someone booking the same slot.
            return CoachErrors.SlotTaken;
        }

        // The coach and the team are told about the request.
        var whoName = string.IsNullOrWhiteSpace(booking.ContactName) ? "A visitor" : booking.ContactName;
        Notifications.NotificationEvents.SessionRequested(unitOfWork, coach.Id, booking.Id, booking.Reference, whoName);
        await unitOfWork.SaveChangesAsync(ct);

        return (await CoachBookingMapper.MapAsync(unitOfWork, [booking], ct))[0];
    }
}

public class SetCoachBookingStatusCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<SetCoachBookingStatusCommand, Result<CoachBookingDto>>
{
    public async Task<Result<CoachBookingDto>> Handle(SetCoachBookingStatusCommand request, CancellationToken ct)
    {
        var booking = await unitOfWork.GetRepository<CoachBooking>().GetTableAsTracking().FirstOrDefaultAsync(b => b.Id == request.Id, ct);
        if (booking is null)
        {
            return CoachErrors.BookingNotFound;
        }

        var moved = booking.MoveTo(request.Status, request.Note);
        if (moved.IsError)
        {
            return moved.Errors;
        }

        // The customer is told; the coach too, because staff made the change.
        Notifications.NotificationEvents.SessionStatusMoved(unitOfWork, booking.CustomerId, booking.CoachId, booking.Reference, request.Status, byStaff: true);
        await unitOfWork.SaveChangesAsync(ct);
        return (await CoachBookingMapper.MapAsync(unitOfWork, [booking], ct))[0];
    }
}

internal static class CoachBookingMapper
{
    public static async Task<List<CoachBookingDto>> MapAsync(IUnitOfWork unitOfWork, IReadOnlyList<CoachBooking> bookings, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var coachIds = bookings.Select(b => b.CoachId).Distinct().ToList();
        var packageIds = bookings.Select(b => b.PackageId).Distinct().ToList();
        var coaches = await db.Set<Coach>().IgnoreQueryFilters().AsNoTracking().Where(c => coachIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.NameEn, ct);
        var packages = await db.Set<CoachPackage>().IgnoreQueryFilters().AsNoTracking().Where(p => packageIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.NameEn, ct);

        return bookings.Select(b => new CoachBookingDto(
            b.Id,
            b.Reference,
            b.CoachId,
            coaches.GetValueOrDefault(b.CoachId) ?? string.Empty,
            b.PackageId,
            packages.GetValueOrDefault(b.PackageId) ?? string.Empty,
            b.CustomerId,
            new CoachBookingContactDto(b.ContactName, b.ContactEmail, b.ContactPhone),
            b.Goal,
            AmmanTime.ToAmman(b.StartsAt).ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
            AmmanTime.ToAmman(b.EndsAt).ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
            b.Status,
            new MoneyDto(b.TotalAmount),
            b.Note,
            AuditDto.From(b))).ToList();
    }
}
