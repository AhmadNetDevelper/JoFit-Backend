using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Services;
using JoFit.Application.Features.Courses.Catalogue;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Customers;
using JoFit.Domain.RentalCategories;
using JoFit.Domain.RentalDepots;
using JoFit.Domain.Rental;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Rental;

// ---- admin: items ---------------------------------------------------------

public sealed record GetAdminRentalItemsQuery(
    int Page, int PageSize, string? Search, IReadOnlyList<string>? Statuses, IReadOnlyList<long>? CategoryIds,
    IReadOnlyList<long>? DepotIds, string? SortField, string? SortDir) : IRequest<Result<PagedResult<AdminRentalItemDto>>>;

public sealed record GetAdminRentalItemQuery(long Id) : IRequest<Result<AdminRentalItemDto>>;

public class GetAdminRentalItemsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdminRentalItemsQuery, Result<PagedResult<AdminRentalItemDto>>>
{
    public async Task<Result<PagedResult<AdminRentalItemDto>>> Handle(GetAdminRentalItemsQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var query = db.Set<RentalItem>().AsNoTracking().AsQueryable();

        if (request.Statuses is { Count: > 0 })
        {
            var statuses = request.Statuses;
            query = query.Where(i => statuses.Contains(i.Status));
        }

        if (request.DepotIds is { Count: > 0 })
        {
            var depots = request.DepotIds;
            query = query.Where(i => depots.Contains(i.DepotId));
        }

        if (request.CategoryIds is { Count: > 0 })
        {
            var categories = request.CategoryIds;
            query = query.Where(i => db.Set<RentalItemCategoryLink>().Any(l => l.RentalItemId == i.Id && categories.Contains(l.RentalCategoryId)));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(i => i.TitleEn.ToLower().Contains(term) || i.TitleAr.ToLower().Contains(term)
                                     || i.SummaryEn.ToLower().Contains(term) || i.SummaryAr.ToLower().Contains(term) || i.Slug.Contains(term));
        }

        var descending = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = request.SortField?.ToLowerInvariant() switch
        {
            "title" => descending ? query.OrderByDescending(i => i.TitleEn) : query.OrderBy(i => i.TitleEn),
            "status" => descending ? query.OrderByDescending(i => i.Status) : query.OrderBy(i => i.Status),
            "totalunits" => descending ? query.OrderByDescending(i => i.TotalUnits) : query.OrderBy(i => i.TotalUnits),
            "updatedat" => descending ? query.OrderByDescending(i => i.LastModifiedUtc ?? i.CreatedAtUtc) : query.OrderBy(i => i.LastModifiedUtc ?? i.CreatedAtUtc),
            _ => query.OrderBy(i => i.Id)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
        var ids = page.Items.Select(i => i.Id).ToList();
        var items = await db.Set<RentalItem>().AsNoTracking().Include(i => i.Rates).Where(i => ids.Contains(i.Id)).ToListAsync(ct);
        var links = await db.Set<RentalItemCategoryLink>().AsNoTracking().Where(l => ids.Contains(l.RentalItemId)).ToListAsync(ct);
        var media = await GalleryService.LoadAsync(unitOfWork, "rental-item", "media", ids, ct);

        var dtos = ids.Select(id => items.First(i => i.Id == id)).Select(i => AdminRentalItemDto.From(
            i,
            links.Where(l => l.RentalItemId == i.Id).Select(l => l.RentalCategoryId).OrderBy(x => x).ToList(),
            media.GetValueOrDefault(i.Id) ?? [])).ToList();
        return new PagedResult<AdminRentalItemDto>(dtos, page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetAdminRentalItemQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdminRentalItemQuery, Result<AdminRentalItemDto>>
{
    public async Task<Result<AdminRentalItemDto>> Handle(GetAdminRentalItemQuery request, CancellationToken ct)
    {
        var exists = await unitOfWork.GetRepository<RentalItem>().GetTableNoTracking().AnyAsync(i => i.Id == request.Id, ct);
        return exists ? await RentalSaving.ToDtoAsync(unitOfWork, request.Id, ct) : RentalErrors.ItemNotFound;
    }
}

// ---- admin: bookings ------------------------------------------------------

public sealed record GetAdminRentalBookingsQuery(
    int Page, int PageSize, string? Search, IReadOnlyList<string>? Statuses, IReadOnlyList<long>? ItemIds,
    long? CustomerId, string? SortField, string? SortDir) : IRequest<Result<PagedResult<AdminRentalBookingDto>>>;

public sealed record GetAdminRentalBookingQuery(long Id) : IRequest<Result<AdminRentalBookingDto>>;

public class GetAdminRentalBookingsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdminRentalBookingsQuery, Result<PagedResult<AdminRentalBookingDto>>>
{
    public async Task<Result<PagedResult<AdminRentalBookingDto>>> Handle(GetAdminRentalBookingsQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var query = db.Set<RentalBooking>().AsNoTracking().AsQueryable();

        if (request.Statuses is { Count: > 0 })
        {
            var statuses = request.Statuses;
            query = query.Where(b => statuses.Contains(b.Status));
        }

        if (request.ItemIds is { Count: > 0 })
        {
            var itemIds = request.ItemIds;
            query = query.Where(b => itemIds.Contains(b.RentalItemId));
        }

        if (request.CustomerId is long customerId)
        {
            query = query.Where(b => b.CustomerId == customerId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            // The reference is RB-<id padded to 5>, so search it by matching the numeric part of the id.
            var numeric = new string(term.Where(char.IsDigit).ToArray());
            var id = long.TryParse(numeric, out var parsed) ? parsed : -1;
            query = query.Where(b => b.Id == id
                || db.Set<RentalItem>().Any(i => i.Id == b.RentalItemId && (i.TitleEn.ToLower().Contains(term) || i.TitleAr.ToLower().Contains(term)))
                || db.Set<Customer>().Any(c => c.Id == b.CustomerId && c.Name.ToLower().Contains(term)));
        }

        var descending = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = request.SortField?.ToLowerInvariant() switch
        {
            "reference" => descending ? query.OrderByDescending(b => b.Id) : query.OrderBy(b => b.Id),
            "status" => descending ? query.OrderByDescending(b => b.Status) : query.OrderBy(b => b.Status),
            "total" => descending ? query.OrderByDescending(b => b.SubtotalAmount) : query.OrderBy(b => b.SubtotalAmount),
            "from" => descending ? query.OrderByDescending(b => b.From) : query.OrderBy(b => b.From),
            _ => query.OrderBy(b => b.From).ThenBy(b => b.Id)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
        var dtos = await RentalSaving.BookingDtosAsync(unitOfWork, page.Items.ToList(), ct);
        return new PagedResult<AdminRentalBookingDto>(dtos, page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetAdminRentalBookingQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdminRentalBookingQuery, Result<AdminRentalBookingDto>>
{
    public async Task<Result<AdminRentalBookingDto>> Handle(GetAdminRentalBookingQuery request, CancellationToken ct)
    {
        var booking = await unitOfWork.GetRepository<RentalBooking>().GetTableNoTracking().FirstOrDefaultAsync(b => b.Id == request.Id, ct);
        return booking is null ? RentalErrors.BookingNotFound : await RentalSaving.BookingDtoAsync(unitOfWork, booking, ct);
    }
}

// ---- storefront -----------------------------------------------------------

public sealed record GetPublicRentalItemsQuery(
    string Language, int Page, int PageSize, string? Search, IReadOnlyList<string>? CategorySlugs,
    long? PriceMin, long? PriceMax, DateOnly? AvailableFrom, DateOnly? AvailableTo, string? Sort)
    : IRequest<Result<PagedResult<PublicRentalItemDto>>>;

public sealed record GetPublicRentalItemBySlugQuery(string Slug, string Language) : IRequest<Result<PublicRentalItemDto>>;

public sealed record GetRentalAvailabilityQuery(long ItemId, DateOnly? From, DateOnly? To) : IRequest<Result<RentalAvailabilityDto>>;

public sealed record GetRentalQuoteQuery(long ItemId, string? Unit, DateOnly? From, DateOnly? To, int Quantity) : IRequest<Result<RentalQuoteDto>>;

public class GetPublicRentalItemsQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicRentalItemsQuery, Result<PagedResult<PublicRentalItemDto>>>
{
    public async Task<Result<PagedResult<PublicRentalItemDto>>> Handle(GetPublicRentalItemsQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var arabic = localization.NormalizeLanguage(request.Language) == "ar";
        var query = db.Set<RentalItem>().AsNoTracking().Where(i => i.Status == "published");

        if (request.CategorySlugs is { Count: > 0 })
        {
            var slugs = request.CategorySlugs;
            query = query.Where(i => db.Set<RentalItemCategoryLink>().Any(l => l.RentalItemId == i.Id
                && db.Set<RentalCategory>().Any(c => c.Id == l.RentalCategoryId && c.Slug != null && slugs.Contains(c.Slug))));
        }

        // Price filters compare the item's lowest rate across units.
        if (request.PriceMin is long min) query = query.Where(i => i.Rates.Min(r => (long?)r.PriceAmount) >= min);
        if (request.PriceMax is long max) query = query.Where(i => i.Rates.Min(r => (long?)r.PriceAmount) <= max);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = arabic
                ? query.Where(i => i.TitleAr.ToLower().Contains(term) || i.SummaryAr.ToLower().Contains(term) || (i.DescriptionAr != null && i.DescriptionAr.ToLower().Contains(term)))
                : query.Where(i => i.TitleEn.ToLower().Contains(term) || i.SummaryEn.ToLower().Contains(term) || (i.DescriptionEn != null && i.DescriptionEn.ToLower().Contains(term)));
        }

        // availableBetween: at least one free unit on every day of the window.
        if (request.AvailableFrom is DateOnly from && request.AvailableTo is DateOnly to && from <= to)
        {
            var candidates = await query.Include(i => i.Rates).ToListAsync(ct);
            var days = await RentalAvailability.ForItemsAsync(unitOfWork, candidates, from, to, ct);
            var freeIds = candidates.Where(i => days[i.Id].All(d => d.AvailableUnits >= 1)).Select(i => i.Id).ToList();
            query = query.Where(i => freeIds.Contains(i.Id));
        }

        query = request.Sort switch
        {
            "price-asc" => query.OrderBy(i => i.Rates.Min(r => r.PriceAmount)).ThenBy(i => i.Id),
            "price-desc" => query.OrderByDescending(i => i.Rates.Min(r => r.PriceAmount)).ThenBy(i => i.Id),
            "title" => arabic ? query.OrderBy(i => i.TitleAr) : query.OrderBy(i => i.TitleEn),
            _ => query.OrderByDescending(i => i.PublishedAtUtc).ThenBy(i => i.Id)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct, PagingExtensions.PublicMaxPageSize);
        var items = await PublicRentalMapper.MapAsync(unitOfWork, localization, request.Language, page.Items.Select(i => i.Id).ToList(), ct);
        return new PagedResult<PublicRentalItemDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetPublicRentalItemBySlugQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicRentalItemBySlugQuery, Result<PublicRentalItemDto>>
{
    public async Task<Result<PublicRentalItemDto>> Handle(GetPublicRentalItemBySlugQuery request, CancellationToken ct)
    {
        var id = await unitOfWork.GetRepository<RentalItem>().GetTableNoTracking()
            .Where(i => i.Slug == request.Slug && i.Status == "published").Select(i => (long?)i.Id).FirstOrDefaultAsync(ct);
        if (id is null)
        {
            return RentalErrors.ItemNotFound;
        }

        return (await PublicRentalMapper.MapAsync(unitOfWork, localization, request.Language, [id.Value], ct))[0];
    }
}

public class GetRentalAvailabilityQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetRentalAvailabilityQuery, Result<RentalAvailabilityDto>>
{
    public async Task<Result<RentalAvailabilityDto>> Handle(GetRentalAvailabilityQuery request, CancellationToken ct)
    {
        var item = await unitOfWork.GetRepository<RentalItem>().GetTableNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.ItemId && i.Status == "published", ct);
        if (item is null)
        {
            return RentalErrors.ItemNotFound;
        }

        // The website's "today" is the UTC date, which can be yesterday in Amman: accept it. The window may reach 45 days.
        var from = request.From ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var to = request.To ?? from.AddDays(30);
        if (to < from)
        {
            return RentalErrors.Field("to", "The end date cannot be before the start date.");
        }

        if (to.DayNumber - from.DayNumber + 1 > RentalAvailability.MaxWindowDays)
        {
            return RentalErrors.Field("to", $"Ask for at most {RentalAvailability.MaxWindowDays} days at a time.");
        }

        var days = await RentalAvailability.DaysAsync(unitOfWork, item, from, to, ct);
        return new RentalAvailabilityDto(item.Id, from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"), days.Min(d => d.AvailableUnits), days);
    }
}

public class GetRentalQuoteQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetRentalQuoteQuery, Result<RentalQuoteDto>>
{
    public async Task<Result<RentalQuoteDto>> Handle(GetRentalQuoteQuery request, CancellationToken ct)
    {
        var item = await unitOfWork.GetRepository<RentalItem>().GetTableNoTracking().Include(i => i.Rates)
            .FirstOrDefaultAsync(i => i.Id == request.ItemId && i.Status == "published", ct);
        if (item is null)
        {
            return RentalErrors.ItemNotFound;
        }

        var unit = request.Unit ?? string.Empty;
        var deposit = item.DepositAmount is long d ? new MoneyDto(d * Math.Max(1, request.Quantity)) : null;
        var lowest = item.Rates.Count == 0 ? 0 : item.Rates.Min(r => r.PriceAmount);

        // Problems are a normal 200 answer, never an error the page would show as a failure.
        RentalQuoteDto Unavailable(string reason) => new(
            item.Id, unit, request.From?.ToString("yyyy-MM-dd") ?? string.Empty, request.To?.ToString("yyyy-MM-dd") ?? string.Empty,
            request.Quantity, 0, new MoneyDto(lowest), new MoneyDto(0), deposit, false, reason);

        var rate = item.Rates.FirstOrDefault(r => r.Unit == unit);
        if (rate is null)
        {
            return Unavailable(UnavailableReasons.UnsupportedUnit);
        }

        if (request.From is not DateOnly from || request.To is not DateOnly to || to < from || request.Quantity < 1
            || to.DayNumber - from.DayNumber + 1 > RentalAvailability.MaxWindowDays)
        {
            return Unavailable(UnavailableReasons.InvalidPeriod);
        }

        var days = await RentalAvailability.DaysAsync(unitOfWork, item, from, to, ct);
        var units = RentalPricing.BillableUnits(unit, from, to, request.Quantity);
        var subtotal = RentalPricing.Subtotal(rate.PriceAmount, units, request.Quantity);
        var available = days.Min(x => x.AvailableUnits) >= request.Quantity;

        return new RentalQuoteDto(
            item.Id, unit, from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"), request.Quantity, units,
            new MoneyDto(rate.PriceAmount), available ? new MoneyDto(subtotal) : new MoneyDto(0), deposit,
            available, available ? null : UnavailableReasons.InsufficientUnits);
    }
}

internal static class PublicRentalMapper
{
    public static async Task<List<PublicRentalItemDto>> MapAsync(
        IUnitOfWork unitOfWork, ILocalizationService localization, string language, IReadOnlyList<long> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var db = unitOfWork.GetDbContext();
        var items = await db.Set<RentalItem>().AsNoTracking().Include(i => i.Rates).Where(i => ids.Contains(i.Id)).ToListAsync(ct);
        var depotIds = items.Select(i => i.DepotId).Distinct().ToList();
        var depots = await db.Set<RentalDepot>().AsNoTracking().Where(d => depotIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, ct);
        var links = await db.Set<RentalItemCategoryLink>().AsNoTracking().Where(l => ids.Contains(l.RentalItemId)).ToListAsync(ct);
        var categoryIds = links.Select(l => l.RentalCategoryId).Distinct().ToList();
        var categories = await db.Set<RentalCategory>().AsNoTracking().Where(c => categoryIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
        var media = await GalleryService.LoadAsync(unitOfWork, "rental-item", "media", ids, ct);

        return ids.Select(id => items.First(i => i.Id == id)).Select(i =>
        {
            var depot = depots.GetValueOrDefault(i.DepotId);
            return PublicRentalItemDto.From(
                i,
                localization,
                language,
                depot is null ? null : new DepotRefDto(depot.Id, localization.GetLocalizedName(depot.NameAr, depot.NameEn, language), localization.GetLocalizedName(depot.CityAr, depot.CityEn, language), depot.CountryCode),
                links.Where(l => l.RentalItemId == i.Id).Select(l => categories.GetValueOrDefault(l.RentalCategoryId)).Where(c => c is not null)
                    .Select(c => new CourseCategoryRefDto(c!.Id, c.Slug ?? string.Empty, localization.GetLocalizedName(c.NameAr, c.NameEn, language))).OrderBy(c => c.Name).ToList(),
                media.GetValueOrDefault(i.Id) ?? []);
        }).ToList();
    }
}
