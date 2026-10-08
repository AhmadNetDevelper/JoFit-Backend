using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Utilities;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Customers;
using JoFit.Domain.Identity.Users;
using JoFit.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Orders;

// ---- the customer's own orders (plain arrays, newest first) ---------------

public sealed record GetMyOrdersQuery(long UserId, string Language) : IRequest<Result<List<AccountOrderDto>>>;

public sealed record GetMyOrderQuery(long UserId, long OrderId, string Language) : IRequest<Result<AccountOrderDto>>;

public class GetMyOrdersQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetMyOrdersQuery, Result<List<AccountOrderDto>>>
{
    public async Task<Result<List<AccountOrderDto>>> Handle(GetMyOrdersQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var customerId = await db.Set<Customer>().AsNoTracking().Where(c => c.UserId == request.UserId).Select(c => (long?)c.Id).FirstOrDefaultAsync(ct);
        var orders = await db.Set<Order>().AsNoTracking().Include(o => o.Lines)
            .Where(o => o.CustomerId == customerId).OrderByDescending(o => o.PlacedAtUtc).ThenByDescending(o => o.Id).ToListAsync(ct);
        return orders.Select(o => AccountOrderDto.From(o, localization, request.Language)).ToList();
    }
}

public class GetMyOrderQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetMyOrderQuery, Result<AccountOrderDto>>
{
    public async Task<Result<AccountOrderDto>> Handle(GetMyOrderQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var customerId = await db.Set<Customer>().AsNoTracking().Where(c => c.UserId == request.UserId).Select(c => (long?)c.Id).FirstOrDefaultAsync(ct);

        // Someone else's order is simply not found.
        var order = await db.Set<Order>().AsNoTracking().Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.CustomerId == customerId, ct);
        return order is null ? OrderErrors.NotFound : AccountOrderDto.From(order, localization, request.Language);
    }
}

// ---- admin ----------------------------------------------------------------

public sealed record GetAdminOrdersQuery(
    int Page, int PageSize, string? Search, IReadOnlyList<string>? Statuses, long? CustomerId,
    DateOnly? PlacedFrom, DateOnly? PlacedTo, string? SortField, string? SortDir) : IRequest<Result<PagedResult<AdminOrderDto>>>;

public sealed record GetAdminOrderQuery(long Id) : IRequest<Result<AdminOrderDto>>;

public class GetAdminOrdersQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdminOrdersQuery, Result<PagedResult<AdminOrderDto>>>
{
    public async Task<Result<PagedResult<AdminOrderDto>>> Handle(GetAdminOrdersQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var query = db.Set<Order>().AsNoTracking().AsQueryable();

        if (request.Statuses is { Count: > 0 })
        {
            var statuses = request.Statuses;
            query = query.Where(o => statuses.Contains(o.Status));
        }

        if (request.CustomerId is long customerId)
        {
            query = query.Where(o => o.CustomerId == customerId);
        }

        // placedBetween is inclusive and compared on the date part in Amman time.
        if (request.PlacedFrom is DateOnly from)
        {
            var start = AmmanTime.At(from, 0).ToUniversalTime();
            query = query.Where(o => o.PlacedAtUtc >= start);
        }

        if (request.PlacedTo is DateOnly to)
        {
            var end = AmmanTime.At(to.AddDays(1), 0).ToUniversalTime();
            query = query.Where(o => o.PlacedAtUtc < end);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            var numeric = new string(term.Where(char.IsDigit).ToArray());
            var id = long.TryParse(numeric, out var parsed) ? parsed : -1;
            query = query.Where(o => o.Id == id
                || db.Set<Customer>().Any(c => c.Id == o.CustomerId && c.Name.ToLower().Contains(term))
                || db.Set<Customer>().Any(c => c.Id == o.CustomerId && db.Set<User>().Any(u => u.Id == c.UserId && u.Email!.ToLower().Contains(term))));
        }

        var ascending = string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        query = request.SortField?.ToLowerInvariant() switch
        {
            "reference" => ascending ? query.OrderBy(o => o.Id) : query.OrderByDescending(o => o.Id),
            "status" => ascending ? query.OrderBy(o => o.Status) : query.OrderByDescending(o => o.Status),
            "grandtotal" => ascending ? query.OrderBy(o => o.GrandTotalAmount) : query.OrderByDescending(o => o.GrandTotalAmount),
            _ => ascending ? query.OrderBy(o => o.PlacedAtUtc) : query.OrderByDescending(o => o.PlacedAtUtc)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
        var ids = page.Items.Select(o => o.Id).ToList();
        var orders = await db.Set<Order>().AsNoTracking().Include(o => o.Lines).Where(o => ids.Contains(o.Id)).ToListAsync(ct);
        var customerIds = orders.Select(o => o.CustomerId).Distinct().ToList();
        var customers = await db.Set<Customer>().AsNoTracking().Where(c => customerIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
        var userIds = customers.Values.Select(c => c.UserId).ToList();
        var emails = await db.Set<User>().AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Email, ct);

        var items = ids.Select(id => orders.First(o => o.Id == id)).Select(o =>
        {
            var customer = customers.GetValueOrDefault(o.CustomerId);
            return AdminOrderDto.From(o, customer?.Name, customer is null ? null : emails.GetValueOrDefault(customer.UserId));
        }).ToList();
        return new PagedResult<AdminOrderDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetAdminOrderQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdminOrderQuery, Result<AdminOrderDto>>
{
    public async Task<Result<AdminOrderDto>> Handle(GetAdminOrderQuery request, CancellationToken ct)
    {
        var exists = await unitOfWork.GetRepository<Order>().GetTableNoTracking().AnyAsync(o => o.Id == request.Id, ct);
        return exists ? await OrderMapper.AdminAsync(unitOfWork, request.Id, ct) : OrderErrors.NotFound;
    }
}
