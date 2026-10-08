using FluentValidation;
using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Utilities;
using JoFit.Application.Features.Auth;
using JoFit.Application.Features.Orders;
using JoFit.Domain.Coaches;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Customers;
using JoFit.Domain.Identity.Users;
using JoFit.Domain.Orders;
using JoFit.Domain.Rental;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Customers;

public sealed record AddressDto(long Id, string Recipient, string Phone, string Line1, string? Line2, string City, string? Notes)
{
    public static AddressDto From(Address a) => new(a.Id, a.Recipient, a.Phone, a.Line1, a.Line2, a.City, a.Notes);
}

public sealed record AccountRentalDto(
    long Id, string Reference, long ItemId, string ItemTitle, string From, string To, string Unit, int Quantity, string Status,
    MoneyDto Subtotal, MoneyDto? Deposit, bool DepositReleased, long? OrderId);

public sealed record AccountSessionDto(
    long Id, string Reference, long CoachId, string CoachName, string PackageName, string StartsAt, string EndsAt, string Status,
    MoneyDto Total, string? Note, bool CanCancel);

public sealed record AccountOverviewDto(
    AccountSessionDto? NextSession,
    AccountRentalDto? NextRental,
    object? NextCohort,
    IReadOnlyList<AccountOrderDto> RecentOrders,
    OverviewCounts Counts,
    int ReviewsToWrite);

public sealed record OverviewCounts(int OpenOrders, int UpcomingSessions, int ActiveRentals);

public sealed record CustomerActivityDto(int Orders, int RentalBookings, int CoachBookings, int Enrolments, int Reviews, int CommunityPosts);

public sealed record AdminCustomerDto(
    long Id,
    string Name,
    string Email,
    string? Phone,
    string Locale,
    bool MarketingOptIn,
    string? City,
    int OrderCount,
    MoneyDto LifetimeValue,
    DateTimeOffset? LastOrderAt,
    DateTimeOffset? LastActiveAt,
    CustomerActivityDto Activity,
    DateTimeOffset CreatedAt);

public sealed record GetMyAddressesQuery(long UserId) : IRequest<Result<List<AddressDto>>>;

public sealed record AddMyAddressCommand(long UserId, string? Recipient, string? Phone, string? Line1, string? Line2, string? City, string? Notes)
    : IRequest<Result<AddressDto>>;

public sealed record RemoveMyAddressCommand(long UserId, long AddressId) : IRequest<Result<Deleted>>;

public sealed record GetMyRentalsQuery(long UserId) : IRequest<Result<List<AccountRentalDto>>>;

public sealed record GetMySessionsQuery(long UserId) : IRequest<Result<List<AccountSessionDto>>>;

public sealed record CancelMySessionCommand(long UserId, long SessionId, string? Reason) : IRequest<Result<Deleted>>;

public sealed record GetMyOverviewQuery(long UserId, string Language) : IRequest<Result<AccountOverviewDto>>;

public sealed record GetAdminCustomersQuery(int Page, int PageSize, string? Search, bool MarketingOptInOnly, string? SortField, string? SortDir)
    : IRequest<Result<PagedResult<AdminCustomerDto>>>;

public sealed record GetAdminCustomerQuery(long Id) : IRequest<Result<AdminCustomerDto>>;

public sealed record UpdateAdminCustomerCommand(long Id, string Name, string Email, string? Phone, string Locale, bool MarketingOptIn)
    : IRequest<Result<AdminCustomerDto>>;

public sealed class UpdateAdminCustomerCommandValidator : AbstractValidator<UpdateAdminCustomerCommand>
{
    public UpdateAdminCustomerCommandValidator()
    {
        RuleFor(x => x.Name).Must(n => AccountRules.CollapseWhitespace(n).Length >= 3).WithMessage("Enter the customer's name (at least 3 characters).");
        RuleFor(x => x.Email).Must(AccountRules.IsValidEmail).WithMessage("An email address we can reach them on.");
        RuleFor(x => x.Phone).OptionalPhone();
        RuleFor(x => x.Locale).Must(l => l is "en" or "ar").WithMessage("Choose en or ar.");
    }
}

internal static class CustomerSupport
{
    public static async Task<long?> CustomerIdOfAsync(IUnitOfWork unitOfWork, long userId, CancellationToken ct)
        => await unitOfWork.GetDbContext().Set<Customer>().AsNoTracking().Where(c => c.UserId == userId).Select(c => (long?)c.Id).FirstOrDefaultAsync(ct);

    public static async Task<List<AccountSessionDto>> SessionsAsync(IUnitOfWork unitOfWork, long customerId, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var bookings = await db.Set<CoachBooking>().AsNoTracking().Where(b => b.CustomerId == customerId).OrderByDescending(b => b.StartsAt).ToListAsync(ct);
        var dtos = await CoachSessionMapper.MapAsync(unitOfWork, bookings, ct);
        return dtos;
    }

    public static async Task<List<AccountRentalDto>> RentalsAsync(IUnitOfWork unitOfWork, long customerId, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var bookings = await db.Set<RentalBooking>().AsNoTracking().Where(b => b.CustomerId == customerId).OrderByDescending(b => b.From).ToListAsync(ct);
        var itemIds = bookings.Select(b => b.RentalItemId).Distinct().ToList();
        var titles = await db.Set<RentalItem>().IgnoreQueryFilters().AsNoTracking().Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, i => i.TitleEn, ct);
        return bookings.Select(b => new AccountRentalDto(
            b.Id, b.Reference, b.RentalItemId, titles.GetValueOrDefault(b.RentalItemId) ?? string.Empty,
            b.From.ToString("yyyy-MM-dd"), b.To.ToString("yyyy-MM-dd"), b.Unit, b.Quantity, b.Status,
            new MoneyDto(b.SubtotalAmount), b.DepositAmount > 0 ? new MoneyDto(b.DepositAmount) : null, b.DepositReleasedAtUtc is not null, b.OrderId)).ToList();
    }
}

/// <summary>Maps coach bookings to the customer's "sessions" view, including whether they can still be cancelled.</summary>
internal static class CoachSessionMapper
{
    public static async Task<List<AccountSessionDto>> MapAsync(IUnitOfWork unitOfWork, IReadOnlyList<CoachBooking> bookings, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var coachIds = bookings.Select(b => b.CoachId).Distinct().ToList();
        var packageIds = bookings.Select(b => b.PackageId).Distinct().ToList();
        var coaches = await db.Set<Coach>().IgnoreQueryFilters().AsNoTracking().Where(c => coachIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.NameEn, ct);
        var packages = await db.Set<CoachPackage>().IgnoreQueryFilters().AsNoTracking().Where(p => packageIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.NameEn, ct);

        return bookings.Select(b => new AccountSessionDto(
            b.Id, b.Reference, b.CoachId, coaches.GetValueOrDefault(b.CoachId) ?? string.Empty, packages.GetValueOrDefault(b.PackageId) ?? string.Empty,
            AmmanTime.ToAmman(b.StartsAt).ToString("yyyy-MM-dd'T'HH:mm:sszzz"), AmmanTime.ToAmman(b.EndsAt).ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
            b.Status, new MoneyDto(b.TotalAmount), b.Note,
            // Cancellable while pending or confirmed, and only while it is still in the future.
            b.Status is CoachBookingStatuses.Pending or CoachBookingStatuses.Confirmed && b.StartsAt > DateTimeOffset.UtcNow)).ToList();
    }
}

public class GetMyAddressesQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetMyAddressesQuery, Result<List<AddressDto>>>
{
    public async Task<Result<List<AddressDto>>> Handle(GetMyAddressesQuery request, CancellationToken ct)
    {
        var customerId = await CustomerSupport.CustomerIdOfAsync(unitOfWork, request.UserId, ct);
        var rows = await unitOfWork.GetRepository<Address>().GetTableNoTracking().Where(a => a.CustomerId == customerId).OrderBy(a => a.Id).ToListAsync(ct);
        return rows.Select(AddressDto.From).ToList();
    }
}

public class AddMyAddressCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<AddMyAddressCommand, Result<AddressDto>>
{
    public async Task<Result<AddressDto>> Handle(AddMyAddressCommand request, CancellationToken ct)
    {
        var customerId = await CustomerSupport.CustomerIdOfAsync(unitOfWork, request.UserId, ct);
        if (customerId is null)
        {
            return CustomerErrors.NotFound;
        }

        var created = Address.Create(customerId.Value, request.Recipient, request.Phone, request.Line1, request.Line2, request.City, request.Notes);
        if (created.IsError)
        {
            return created.Errors;
        }

        await unitOfWork.GetRepository<Address>().AddAsync(created.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return AddressDto.From(created.Value);
    }
}

public class RemoveMyAddressCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<RemoveMyAddressCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(RemoveMyAddressCommand request, CancellationToken ct)
    {
        var customerId = await CustomerSupport.CustomerIdOfAsync(unitOfWork, request.UserId, ct);
        var repository = unitOfWork.GetRepository<Address>();
        var address = await repository.GetTableAsTracking().FirstOrDefaultAsync(a => a.Id == request.AddressId && a.CustomerId == customerId, ct);
        if (address is null)
        {
            return Error.NotFound("Address.NotFound", "That address does not exist.");
        }

        // Orders keep their own copy of the address, so removing it never changes an order.
        repository.DeleteAsync(address);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}

public class GetMyRentalsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetMyRentalsQuery, Result<List<AccountRentalDto>>>
{
    public async Task<Result<List<AccountRentalDto>>> Handle(GetMyRentalsQuery request, CancellationToken ct)
    {
        var customerId = await CustomerSupport.CustomerIdOfAsync(unitOfWork, request.UserId, ct);
        return customerId is null ? new List<AccountRentalDto>() : await CustomerSupport.RentalsAsync(unitOfWork, customerId.Value, ct);
    }
}

public class GetMySessionsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetMySessionsQuery, Result<List<AccountSessionDto>>>
{
    public async Task<Result<List<AccountSessionDto>>> Handle(GetMySessionsQuery request, CancellationToken ct)
    {
        var customerId = await CustomerSupport.CustomerIdOfAsync(unitOfWork, request.UserId, ct);
        return customerId is null ? new List<AccountSessionDto>() : await CustomerSupport.SessionsAsync(unitOfWork, customerId.Value, ct);
    }
}

public class CancelMySessionCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CancelMySessionCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(CancelMySessionCommand request, CancellationToken ct)
    {
        // The reason is checked before ownership, so a bad reason answers 422 even for someone else's session.
        var reason = request.Reason?.Trim() ?? string.Empty;
        if (reason.Length < 3)
        {
            return Error.Validation("Session.ReasonRequired", "Tell us why you are cancelling (at least 3 characters).").ForField("reason");
        }

        var customerId = await CustomerSupport.CustomerIdOfAsync(unitOfWork, request.UserId, ct);
        var booking = await unitOfWork.GetRepository<CoachBooking>().GetTableAsTracking()
            .FirstOrDefaultAsync(b => b.Id == request.SessionId && b.CustomerId == customerId, ct);
        if (booking is null)
        {
            return CoachErrors.BookingNotFound;
        }

        if (booking.Status is not (CoachBookingStatuses.Pending or CoachBookingStatuses.Confirmed) || booking.StartsAt <= DateTimeOffset.UtcNow)
        {
            return Error.Conflict("Session.NotCancellable", "This session can no longer be cancelled.");
        }

        var moved = booking.MoveTo(CoachBookingStatuses.Cancelled, reason);
        if (moved.IsError)
        {
            return moved.Errors;
        }

        Notifications.NotificationEvents.SessionCancelledByClient(unitOfWork, booking.CoachId, booking.Reference);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}

public class GetMyOverviewQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetMyOverviewQuery, Result<AccountOverviewDto>>
{
    public async Task<Result<AccountOverviewDto>> Handle(GetMyOverviewQuery request, CancellationToken ct)
    {
        var customerId = await CustomerSupport.CustomerIdOfAsync(unitOfWork, request.UserId, ct);
        if (customerId is null)
        {
            return CustomerErrors.NotFound;
        }

        var db = unitOfWork.GetDbContext();
        var now = DateTimeOffset.UtcNow;
        var today = AmmanTime.Today;

        var sessions = await CustomerSupport.SessionsAsync(unitOfWork, customerId.Value, ct);
        var upcoming = sessions.Where(s => s.Status is CoachBookingStatuses.Pending or CoachBookingStatuses.Confirmed
                                           && DateTimeOffset.Parse(s.StartsAt) > now).OrderBy(s => DateTimeOffset.Parse(s.StartsAt)).ToList();

        var rentals = await CustomerSupport.RentalsAsync(unitOfWork, customerId.Value, ct);
        var nextCohort = (await db.Set<JoFit.Domain.Enrolments.Enrolment>().AsNoTracking()
            .Where(e => e.CustomerId == customerId && e.CohortStartsOn >= today && JoFit.Domain.Enrolments.EnrolmentStatuses.Live.Contains(e.Status))
            .OrderBy(e => e.CohortStartsOn).FirstOrDefaultAsync(ct));
        var activeRentals = rentals.Where(r => r.Status is not (RentalBookingStatuses.Cancelled or RentalBookingStatuses.Returned)
                                               && DateOnly.Parse(r.To) >= today).OrderBy(r => r.From).ToList();

        var recent = await db.Set<Order>().AsNoTracking().Include(o => o.Lines)
            .Where(o => o.CustomerId == customerId).OrderByDescending(o => o.PlacedAtUtc).Take(3).ToListAsync(ct);
        string[] open = [OrderStatuses.Pending, OrderStatuses.Confirmed, OrderStatuses.Processing, OrderStatuses.Shipped];
        var openOrders = await db.Set<Order>().CountAsync(o => o.CustomerId == customerId && open.Contains(o.Status), ct);

        return new AccountOverviewDto(
            upcoming.FirstOrDefault(),
            activeRentals.FirstOrDefault(),
            nextCohort is null ? null : new { enrolmentId = nextCohort.Id, reference = nextCohort.Reference, courseId = nextCohort.CourseId, cohortStartsOn = nextCohort.CohortStartsOn!.Value.ToString("yyyy-MM-dd"), status = nextCohort.Status },
            recent.Select(o => AccountOrderDto.From(o, localization, request.Language)).ToList(),
            new OverviewCounts(openOrders, upcoming.Count, activeRentals.Count),
            0);
    }
}

// ---- admin customers ------------------------------------------------------

internal static class AdminCustomerMapper
{
    public static async Task<List<AdminCustomerDto>> MapAsync(IUnitOfWork unitOfWork, IReadOnlyList<Customer> customers, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var ids = customers.Select(c => c.Id).ToList();
        var userIds = customers.Select(c => c.UserId).ToList();
        var emails = await db.Set<User>().AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Email, ct);

        var orders = await db.Set<Order>().AsNoTracking().Where(o => ids.Contains(o.CustomerId))
            .Select(o => new { o.CustomerId, o.Status, o.GrandTotalAmount, o.DepositsTotalAmount, o.PlacedAtUtc }).ToListAsync(ct);
        var rentals = await db.Set<RentalBooking>().AsNoTracking().Where(b => b.CustomerId != null && ids.Contains(b.CustomerId.Value))
            .GroupBy(b => b.CustomerId!.Value).Select(g => new { Id = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, ct);
        var sessions = await db.Set<CoachBooking>().AsNoTracking().Where(b => b.CustomerId != null && ids.Contains(b.CustomerId.Value))
            .Select(b => new { CustomerId = b.CustomerId!.Value, b.Status, b.TotalAmount, b.CreatedAtUtc }).ToListAsync(ct);
        var enrolmentCounts = await db.Set<JoFit.Domain.Enrolments.Enrolment>().AsNoTracking().Where(e => ids.Contains(e.CustomerId))
            .GroupBy(e => e.CustomerId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var enrolmentValue = await db.Set<JoFit.Domain.Enrolments.Enrolment>().AsNoTracking()
            .Where(e => ids.Contains(e.CustomerId) && (e.Status == "active" || e.Status == "completed"))
            .GroupBy(e => e.CustomerId).Select(g => new { g.Key, Sum = g.Sum(e => e.AmountPaidAmount) }).ToDictionaryAsync(x => x.Key, x => x.Sum, ct);
        var cities = await db.Set<Address>().AsNoTracking().Where(a => ids.Contains(a.CustomerId)).OrderBy(a => a.Id).ToListAsync(ct);

        string[] earning = [OrderStatuses.Confirmed, OrderStatuses.Processing, OrderStatuses.Shipped, OrderStatuses.Completed];
        string[] sessionEarning = [CoachBookingStatuses.Confirmed, CoachBookingStatuses.Completed];

        return customers.Select(c =>
        {
            var myOrders = orders.Where(o => o.CustomerId == c.Id).ToList();
            var mySessions = sessions.Where(s => s.CustomerId == c.Id).ToList();
            var value = myOrders.Where(o => earning.Contains(o.Status)).Sum(o => o.GrandTotalAmount - o.DepositsTotalAmount)
                        + mySessions.Where(s => sessionEarning.Contains(s.Status)).Sum(s => s.TotalAmount)
                        + enrolmentValue.GetValueOrDefault(c.Id);
            var lastOrder = myOrders.Count == 0 ? (DateTimeOffset?)null : myOrders.Max(o => o.PlacedAtUtc);
            var lastActive = new[] { lastOrder, mySessions.Count == 0 ? null : mySessions.Max(s => s.CreatedAtUtc), c.LastModifiedUtc, c.CreatedAtUtc }
                .Where(d => d is not null).Max();
            return new AdminCustomerDto(
                c.Id, c.Name, emails.GetValueOrDefault(c.UserId) ?? string.Empty, c.Phone, c.Locale, c.MarketingOptIn,
                cities.LastOrDefault(a => a.CustomerId == c.Id)?.City,
                myOrders.Count, new MoneyDto(value), lastOrder, lastActive,
                new CustomerActivityDto(myOrders.Count, rentals.GetValueOrDefault(c.Id), mySessions.Count, enrolmentCounts.GetValueOrDefault(c.Id), 0, 0),
                c.CreatedAtUtc);
        }).ToList();
    }
}

public class GetAdminCustomersQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdminCustomersQuery, Result<PagedResult<AdminCustomerDto>>>
{
    public async Task<Result<PagedResult<AdminCustomerDto>>> Handle(GetAdminCustomersQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var query = db.Set<Customer>().AsNoTracking().AsQueryable();

        // marketingOptInOnly=false means no filter.
        if (request.MarketingOptInOnly)
        {
            query = query.Where(c => c.MarketingOptIn);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(term) || (c.Phone != null && c.Phone.Contains(term))
                                     || db.Set<User>().Any(u => u.Id == c.UserId && u.Email!.ToLower().Contains(term)));
        }

        var descending = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = request.SortField?.ToLowerInvariant() switch
        {
            "createdat" => descending ? query.OrderByDescending(c => c.CreatedAtUtc) : query.OrderBy(c => c.CreatedAtUtc),
            "ordercount" => descending
                ? query.OrderByDescending(c => db.Set<Order>().Count(o => o.CustomerId == c.Id))
                : query.OrderBy(c => db.Set<Order>().Count(o => o.CustomerId == c.Id)),
            _ => descending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
        return new PagedResult<AdminCustomerDto>(await AdminCustomerMapper.MapAsync(unitOfWork, page.Items.ToList(), ct), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetAdminCustomerQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdminCustomerQuery, Result<AdminCustomerDto>>
{
    public async Task<Result<AdminCustomerDto>> Handle(GetAdminCustomerQuery request, CancellationToken ct)
    {
        var customer = await unitOfWork.GetRepository<Customer>().GetTableNoTracking().FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        return customer is null ? CustomerErrors.NotFound : (await AdminCustomerMapper.MapAsync(unitOfWork, [customer], ct))[0];
    }
}

public class UpdateAdminCustomerCommandHandler(IUnitOfWork unitOfWork, IAccountService accounts)
    : IRequestHandler<UpdateAdminCustomerCommand, Result<AdminCustomerDto>>
{
    public async Task<Result<AdminCustomerDto>> Handle(UpdateAdminCustomerCommand request, CancellationToken ct)
    {
        var customer = await unitOfWork.GetRepository<Customer>().GetTableAsTracking().FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (customer is null)
        {
            return CustomerErrors.NotFound;
        }

        // Changing the email changes the login email; a duplicate is a 422 on the field here (a 409 only for the storefront).
        var changed = await accounts.SetLoginEmailAsync(customer.UserId, request.Email, ct);
        if (changed.IsError)
        {
            return changed.Errors;
        }

        customer.UpdateProfile(AccountRules.CollapseWhitespace(request.Name), string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim());
        customer.UpdatePreferences(request.Locale, request.MarketingOptIn);
        await unitOfWork.SaveChangesAsync(ct);
        return (await AdminCustomerMapper.MapAsync(unitOfWork, [customer], ct))[0];
    }
}
