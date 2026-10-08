using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Services;
using JoFit.Application.Common.Utilities;
using JoFit.Domain.CoachSpecialisations;
using JoFit.Domain.Coaches;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Customers;
using JoFit.Domain.Locations;
using JoFit.Domain.Sports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Coaches;

// ---- admin ----------------------------------------------------------------

public sealed record GetAdminCoachesQuery(
    int Page, int PageSize, string? Search, IReadOnlyList<string>? Statuses, IReadOnlyList<long>? SportIds,
    IReadOnlyList<long>? Specialisations, IReadOnlyList<long>? LocationIds, string? SortField, string? SortDir)
    : IRequest<Result<PagedResult<AdminCoachDto>>>;

public sealed record GetAdminCoachQuery(long Id) : IRequest<Result<AdminCoachDto>>;

public sealed record GetCoachLocationsQuery : IRequest<Result<List<LocationDto>>>;

public sealed record GetAdminCoachBookingsQuery(
    int Page, int PageSize, string? Search, IReadOnlyList<string>? Statuses, IReadOnlyList<long>? CoachIds,
    long? CustomerId, bool UpcomingOnly, string? SortField, string? SortDir) : IRequest<Result<PagedResult<CoachBookingDto>>>;

public sealed record GetAdminCoachBookingQuery(long Id) : IRequest<Result<CoachBookingDto>>;

public class GetAdminCoachesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdminCoachesQuery, Result<PagedResult<AdminCoachDto>>>
{
    public async Task<Result<PagedResult<AdminCoachDto>>> Handle(GetAdminCoachesQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var query = db.Set<Coach>().AsNoTracking().AsQueryable();

        if (request.Statuses is { Count: > 0 })
        {
            var statuses = request.Statuses;
            query = query.Where(c => statuses.Contains(c.VerificationStatus));
        }

        if (request.LocationIds is { Count: > 0 })
        {
            var locations = request.LocationIds;
            query = query.Where(c => locations.Contains(c.LocationId));
        }

        if (request.SportIds is { Count: > 0 })
        {
            var sports = request.SportIds;
            query = query.Where(c => db.Set<CoachSportLink>().Any(l => l.CoachId == c.Id && sports.Contains(l.SportId)));
        }

        if (request.Specialisations is { Count: > 0 })
        {
            var specs = request.Specialisations;
            query = query.Where(c => db.Set<CoachSpecialisationLink>().Any(l => l.CoachId == c.Id && specs.Contains(l.CoachSpecialisationId)));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(c => c.NameEn.ToLower().Contains(term) || c.NameAr.ToLower().Contains(term)
                                     || c.HeadlineEn.ToLower().Contains(term) || c.HeadlineAr.ToLower().Contains(term)
                                     || c.Slug.Contains(term) || c.Email.Contains(term));
        }

        var descending = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = request.SortField?.ToLowerInvariant() switch
        {
            "verificationstatus" => descending ? query.OrderByDescending(c => c.VerificationStatus) : query.OrderBy(c => c.VerificationStatus),
            "updatedat" => descending ? query.OrderByDescending(c => c.LastModifiedUtc ?? c.CreatedAtUtc) : query.OrderBy(c => c.LastModifiedUtc ?? c.CreatedAtUtc),
            _ => descending ? query.OrderByDescending(c => c.NameEn) : query.OrderBy(c => c.NameEn)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
        var ids = page.Items.Select(c => c.Id).ToList();
        var coaches = await db.Set<Coach>().AsNoTracking().Include(c => c.Packages).Include(c => c.Certifications).Where(c => ids.Contains(c.Id)).ToListAsync(ct);
        var sportLinks = await db.Set<CoachSportLink>().AsNoTracking().Where(l => ids.Contains(l.CoachId)).ToListAsync(ct);
        var specLinks = await db.Set<CoachSpecialisationLink>().AsNoTracking().Where(l => ids.Contains(l.CoachId)).ToListAsync(ct);
        var avatars = await GalleryService.LoadAsync(unitOfWork, "coach", "avatar", ids, ct);
        var galleries = await GalleryService.LoadAsync(unitOfWork, "coach", "gallery", ids, ct);
        var completed = await db.Set<CoachBooking>().AsNoTracking()
            .Where(b => ids.Contains(b.CoachId) && b.Status == CoachBookingStatuses.Completed)
            .GroupBy(b => b.CoachId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var items = ids.Select(id => coaches.First(c => c.Id == id)).Select(c => AdminCoachDto.From(
            c,
            sportLinks.Where(l => l.CoachId == c.Id).Select(l => l.SportId).OrderBy(x => x).ToList(),
            specLinks.Where(l => l.CoachId == c.Id).Select(l => l.CoachSpecialisationId).OrderBy(x => x).ToList(),
            avatars.GetValueOrDefault(c.Id)?.FirstOrDefault(),
            galleries.GetValueOrDefault(c.Id) ?? [],
            completed.GetValueOrDefault(c.Id))).ToList();
        return new PagedResult<AdminCoachDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetAdminCoachQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdminCoachQuery, Result<AdminCoachDto>>
{
    public async Task<Result<AdminCoachDto>> Handle(GetAdminCoachQuery request, CancellationToken ct)
    {
        var exists = await unitOfWork.GetRepository<Coach>().GetTableNoTracking().AnyAsync(c => c.Id == request.Id, ct);
        return exists ? await CoachSaving.ToDtoAsync(unitOfWork, request.Id, ct) : CoachErrors.NotFound;
    }
}

public class GetCoachLocationsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetCoachLocationsQuery, Result<List<LocationDto>>>
{
    public async Task<Result<List<LocationDto>>> Handle(GetCoachLocationsQuery request, CancellationToken ct)
        => (await unitOfWork.GetRepository<Location>().GetTableNoTracking().OrderBy(l => l.NameEn).ToListAsync(ct))
            .Select(l => new LocationDto(l.Id, l.Slug, new LocalizedText(l.NameEn, l.NameAr))).ToList();
}

public class GetAdminCoachBookingsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdminCoachBookingsQuery, Result<PagedResult<CoachBookingDto>>>
{
    public async Task<Result<PagedResult<CoachBookingDto>>> Handle(GetAdminCoachBookingsQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var query = db.Set<CoachBooking>().AsNoTracking().AsQueryable();

        if (request.Statuses is { Count: > 0 })
        {
            var statuses = request.Statuses;
            query = query.Where(b => statuses.Contains(b.Status));
        }

        if (request.CoachIds is { Count: > 0 })
        {
            var coachIds = request.CoachIds;
            query = query.Where(b => coachIds.Contains(b.CoachId));
        }

        if (request.CustomerId is long customerId)
        {
            query = query.Where(b => b.CustomerId == customerId);
        }

        // upcomingOnly=false means no filter. Upcoming is the date part of the start (Amman) from today on.
        if (request.UpcomingOnly)
        {
            var startOfToday = AmmanTime.At(AmmanTime.Today, 0).ToUniversalTime();
            query = query.Where(b => b.StartsAt >= startOfToday);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            var numeric = new string(term.Where(char.IsDigit).ToArray());
            var id = long.TryParse(numeric, out var parsed) ? parsed : -1;
            query = query.Where(b => b.Id == id || b.ContactName.ToLower().Contains(term) || b.ContactEmail.ToLower().Contains(term)
                                     || db.Set<Coach>().Any(c => c.Id == b.CoachId && (c.NameEn.ToLower().Contains(term) || c.NameAr.ToLower().Contains(term))));
        }

        var ascending = string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        query = request.SortField?.ToLowerInvariant() switch
        {
            "reference" => ascending ? query.OrderBy(b => b.Id) : query.OrderByDescending(b => b.Id),
            "status" => ascending ? query.OrderBy(b => b.Status) : query.OrderByDescending(b => b.Status),
            "total" => ascending ? query.OrderBy(b => b.TotalAmount) : query.OrderByDescending(b => b.TotalAmount),
            _ => ascending ? query.OrderBy(b => b.StartsAt) : query.OrderByDescending(b => b.StartsAt)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
        return new PagedResult<CoachBookingDto>(await CoachBookingMapper.MapAsync(unitOfWork, page.Items.ToList(), ct), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetAdminCoachBookingQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdminCoachBookingQuery, Result<CoachBookingDto>>
{
    public async Task<Result<CoachBookingDto>> Handle(GetAdminCoachBookingQuery request, CancellationToken ct)
    {
        var booking = await unitOfWork.GetRepository<CoachBooking>().GetTableNoTracking().FirstOrDefaultAsync(b => b.Id == request.Id, ct);
        return booking is null ? CoachErrors.BookingNotFound : (await CoachBookingMapper.MapAsync(unitOfWork, [booking], ct))[0];
    }
}

// ---- storefront -----------------------------------------------------------

public sealed record GetPublicCoachesQuery(
    string Language, int Page, int PageSize, string? Search, IReadOnlyList<string>? SportSlugs,
    IReadOnlyList<long>? Specialisations, IReadOnlyList<long>? LocationIds, IReadOnlyList<string>? Languages, string? Sort)
    : IRequest<Result<PagedResult<PublicCoachDto>>>;

public sealed record GetPublicCoachesByIdsQuery(IReadOnlyList<long> Ids, string Language) : IRequest<Result<List<PublicCoachDto>>>;

public sealed record GetPublicCoachBySlugQuery(string Slug, string Language) : IRequest<Result<PublicCoachDto>>;

public sealed record GetCoachAvailabilityQuery(long CoachId, DateOnly? From, DateOnly? To) : IRequest<Result<List<CoachSlotDto>>>;

public class GetPublicCoachesQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicCoachesQuery, Result<PagedResult<PublicCoachDto>>>
{
    public async Task<Result<PagedResult<PublicCoachDto>>> Handle(GetPublicCoachesQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var arabic = localization.NormalizeLanguage(request.Language) == "ar";
        // Only approved coaches are listed publicly.
        var query = db.Set<Coach>().AsNoTracking().Where(c => c.VerificationStatus == CoachVerification.Approved);

        if (request.SportSlugs is { Count: > 0 })
        {
            var slugs = request.SportSlugs;
            query = query.Where(c => db.Set<CoachSportLink>().Any(l => l.CoachId == c.Id
                && db.Set<Sport>().Any(s => s.Id == l.SportId && s.Slug != null && slugs.Contains(s.Slug))));
        }

        if (request.Specialisations is { Count: > 0 })
        {
            var specs = request.Specialisations;
            query = query.Where(c => db.Set<CoachSpecialisationLink>().Any(l => l.CoachId == c.Id && specs.Contains(l.CoachSpecialisationId)));
        }

        if (request.LocationIds is { Count: > 0 })
        {
            var locations = request.LocationIds;
            query = query.Where(c => locations.Contains(c.LocationId));
        }

        if (request.Languages is { Count: > 0 })
        {
            var languages = request.Languages;
            query = query.Where(c => languages.Any(l => ("," + c.Languages + ",").Contains("," + l + ",")));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = arabic
                ? query.Where(c => c.NameAr.ToLower().Contains(term) || c.HeadlineAr.ToLower().Contains(term) || (c.BioAr != null && c.BioAr.ToLower().Contains(term)))
                : query.Where(c => c.NameEn.ToLower().Contains(term) || c.HeadlineEn.ToLower().Contains(term) || (c.BioEn != null && c.BioEn.ToLower().Contains(term)));
        }

        query = request.Sort switch
        {
            "experience-desc" => query.OrderByDescending(c => c.YearsOfExperience).ThenBy(c => c.Id),
            "price-asc" => query.OrderBy(c => c.Packages.Min(p => (long?)p.PriceAmount)).ThenBy(c => c.Id),
            _ => arabic ? query.OrderBy(c => c.NameAr) : query.OrderBy(c => c.NameEn)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct, PagingExtensions.PublicMaxPageSize);
        var items = await PublicCoachMapper.MapAsync(unitOfWork, localization, request.Language, page.Items.Select(c => c.Id).ToList(), ct);
        return new PagedResult<PublicCoachDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetPublicCoachesByIdsQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicCoachesByIdsQuery, Result<List<PublicCoachDto>>>
{
    public async Task<Result<List<PublicCoachDto>>> Handle(GetPublicCoachesByIdsQuery request, CancellationToken ct)
    {
        var listed = await unitOfWork.GetRepository<Coach>().GetTableNoTracking()
            .Where(c => request.Ids.Contains(c.Id) && c.VerificationStatus == CoachVerification.Approved).Select(c => c.Id).ToListAsync(ct);
        // Keep the order the caller asked for.
        var ids = request.Ids.Where(listed.Contains).Distinct().ToList();
        return await PublicCoachMapper.MapAsync(unitOfWork, localization, request.Language, ids, ct);
    }
}

public class GetPublicCoachBySlugQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicCoachBySlugQuery, Result<PublicCoachDto>>
{
    public async Task<Result<PublicCoachDto>> Handle(GetPublicCoachBySlugQuery request, CancellationToken ct)
    {
        var id = await unitOfWork.GetRepository<Coach>().GetTableNoTracking()
            .Where(c => c.Slug == request.Slug && c.VerificationStatus == CoachVerification.Approved).Select(c => (long?)c.Id).FirstOrDefaultAsync(ct);
        if (id is null)
        {
            return CoachErrors.NotFound;
        }

        return (await PublicCoachMapper.MapAsync(unitOfWork, localization, request.Language, [id.Value], ct))[0];
    }
}

public class GetCoachAvailabilityQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetCoachAvailabilityQuery, Result<List<CoachSlotDto>>>
{
    public async Task<Result<List<CoachSlotDto>>> Handle(GetCoachAvailabilityQuery request, CancellationToken ct)
    {
        var coach = await unitOfWork.GetRepository<Coach>().GetTableNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CoachId && c.VerificationStatus == CoachVerification.Approved, ct);
        if (coach is null)
        {
            return CoachErrors.NotFound;
        }

        var from = request.From ?? AmmanTime.Today;
        var to = request.To ?? from.AddDays(14);
        if (to < from)
        {
            return CoachErrors.Field("to", "The end date cannot be before the start date.");
        }

        if (to.DayNumber - from.DayNumber + 1 > CoachSlots.MaxWindowDays)
        {
            return CoachErrors.Field("to", $"Ask for at most {CoachSlots.MaxWindowDays} days at a time.");
        }

        var windowStart = AmmanTime.At(from, 0).ToUniversalTime();
        var windowEnd = AmmanTime.At(to.AddDays(1), 0).ToUniversalTime();
        var live = CoachBookingStatuses.Live;
        var booked = await unitOfWork.GetRepository<CoachBooking>().GetTableNoTracking()
            .Where(b => b.CoachId == coach.Id && live.Contains(b.Status) && b.StartsAt < windowEnd && b.EndsAt > windowStart)
            .Select(b => new { b.StartsAt, b.EndsAt }).ToListAsync(ct);

        return CoachSlots.Offered(coach, from, to)
            .Select(s => (Start: AmmanTime.At(s.Date, s.Hour), s.Date, s.Hour))
            .Where(s => !booked.Any(b => b.StartsAt < s.Start.AddHours(1) && b.EndsAt > s.Start))
            .Select(s => new CoachSlotDto(
                CoachSlots.IdOf(coach.Id, s.Date, s.Hour),
                s.Start.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
                s.Start.AddHours(1).ToString("yyyy-MM-dd'T'HH:mm:sszzz")))
            .ToList();
    }
}

internal static class PublicCoachMapper
{
    public static async Task<List<PublicCoachDto>> MapAsync(
        IUnitOfWork unitOfWork, ILocalizationService localization, string language, IReadOnlyList<long> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var db = unitOfWork.GetDbContext();
        var coaches = await db.Set<Coach>().AsNoTracking().Include(c => c.Packages).Include(c => c.Certifications).Where(c => ids.Contains(c.Id)).ToListAsync(ct);
        var locationIds = coaches.Select(c => c.LocationId).Distinct().ToList();
        var locations = await db.Set<Location>().AsNoTracking().Where(l => locationIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, ct);
        var sportLinks = await db.Set<CoachSportLink>().AsNoTracking().Where(l => ids.Contains(l.CoachId)).ToListAsync(ct);
        var specLinks = await db.Set<CoachSpecialisationLink>().AsNoTracking().Where(l => ids.Contains(l.CoachId)).ToListAsync(ct);
        var sports = await db.Set<Sport>().AsNoTracking().Where(s => sportLinks.Select(l => l.SportId).Contains(s.Id)).ToDictionaryAsync(s => s.Id, ct);
        var specs = await db.Set<CoachSpecialisation>().AsNoTracking().Where(s => specLinks.Select(l => l.CoachSpecialisationId).Contains(s.Id)).ToDictionaryAsync(s => s.Id, ct);
        var avatars = await GalleryService.LoadAsync(unitOfWork, "coach", "avatar", ids, ct);
        var galleries = await GalleryService.LoadAsync(unitOfWork, "coach", "gallery", ids, ct);
        var completed = await db.Set<CoachBooking>().AsNoTracking()
            .Where(b => ids.Contains(b.CoachId) && b.Status == CoachBookingStatuses.Completed)
            .GroupBy(b => b.CoachId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        string Pick(string ar, string en) => localization.GetLocalizedName(ar, en, language);

        return ids.Select(id => coaches.First(c => c.Id == id)).Select(c =>
        {
            var location = locations.GetValueOrDefault(c.LocationId);
            return new PublicCoachDto(
                c.Id,
                c.Slug,
                Pick(c.NameAr, c.NameEn),
                Pick(c.HeadlineAr, c.HeadlineEn),
                c.BioEn is null || c.BioAr is null ? null : Pick(c.BioAr, c.BioEn),
                location is null ? null : new LocationRef(location.Id, location.Slug, Pick(location.NameAr, location.NameEn)),
                sportLinks.Where(l => l.CoachId == c.Id).Select(l => sports.GetValueOrDefault(l.SportId)).Where(s => s is not null)
                    .Select(s => new PublicRef(s!.Id, s.Slug ?? string.Empty, Pick(s.NameAr, s.NameEn))).OrderBy(s => s.Name).ToList(),
                specLinks.Where(l => l.CoachId == c.Id).Select(l => specs.GetValueOrDefault(l.CoachSpecialisationId)).Where(s => s is not null)
                    .Select(s => Pick(s!.NameAr, s.NameEn)).OrderBy(s => s).ToList(),
                c.LanguageList,
                c.YearsOfExperience,
                c.ResponseTimeHours,
                c.SessionRateAmount is long r ? new MoneyDto(r) : null,
                c.Packages.OrderBy(p => p.Position).Select(p => new PublicPackageDto(p.Id, Pick(p.NameAr, p.NameEn), p.DurationMinutes, p.SessionCount, p.Mode, new MoneyDto(p.PriceAmount), p.CompareAtAmount is long cmp ? new MoneyDto(cmp) : null)).ToList(),
                // Only verified certifications are shown to visitors.
                c.Certifications.Where(x => x.IsVerified).Select(x => new PublicCertificationDto(Pick(x.NameAr, x.NameEn), Pick(x.IssuerAr, x.IssuerEn), x.Year)).ToList(),
                avatars.GetValueOrDefault(c.Id)?.FirstOrDefault(),
                galleries.GetValueOrDefault(c.Id) ?? [],
                completed.GetValueOrDefault(c.Id));
        }).ToList();
    }
}
