using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Rental;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Customers;
using JoFit.Domain.Orders;
using JoFit.Domain.Products;
using JoFit.Domain.Rental;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Orders;

/// <summary>A basket line. Ids only: the API prices everything itself and never trusts a client price.</summary>
public sealed record OrderLineInput(
    string? Kind, long? ProductId, long? VariantId, long? RentalItemId, string? Unit, DateOnly? From, DateOnly? To, int Quantity);

public sealed record PlaceOrderCommand(long UserId, IReadOnlyList<OrderLineInput> Lines, string? Fulfilment, long? AddressId, string? Notes)
    : IRequest<Result<PlacedOrderDto>>;

public sealed record CancelMyOrderCommand(long UserId, long OrderId) : IRequest<Result<Deleted>>;

public sealed record SetOrderStatusCommand(long OrderId, string Status) : IRequest<Result<AdminOrderDto>>;

/// <summary>Placeholder rates the client still has to confirm.</summary>
public static class OrderRates
{
    public const long DeliveryFeeAmount = 3000; // 3.000 JOD
    public const decimal TaxRate = 0.16m;
    public const int MaxLineQuantity = 10;
}

internal static class OrderSupport
{
    /// <summary>Takes or returns stock of every shop line, one atomic statement per variant. False if any would go below 0.</summary>
    public static async Task<bool> MoveStockAsync(IUnitOfWork unitOfWork, Order order, int direction, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        foreach (var line in order.Lines.Where(l => l.Kind == OrderKinds.Shop && l.VariantId != null))
        {
            var variantId = line.VariantId!.Value;
            var delta = direction * line.Quantity;
            var changed = await db.Set<ProductVariant>()
                .Where(v => v.Id == variantId && v.StockOnHand + delta >= 0)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockOnHand, v => v.StockOnHand + delta), ct);
            if (changed == 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Cancels every hire linked to the order (a cancelled order releases its units).</summary>
    public static async Task CancelRentalsAsync(IUnitOfWork unitOfWork, Order order, CancellationToken ct)
    {
        var bookingIds = order.Lines.Where(l => l.RentalBookingId != null).Select(l => l.RentalBookingId!.Value).ToList();
        if (bookingIds.Count == 0)
        {
            return;
        }

        var bookings = await unitOfWork.GetRepository<RentalBooking>().GetTableAsTracking().Where(b => bookingIds.Contains(b.Id)).ToListAsync(ct);
        bookings.ForEach(b => b.CancelWithOrder());
    }
}

public class PlaceOrderCommandHandler(IUnitOfWork unitOfWork, ISender sender)
    : IRequestHandler<PlaceOrderCommand, Result<PlacedOrderDto>>
{
    public async Task<Result<PlacedOrderDto>> Handle(PlaceOrderCommand request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var customerId = await db.Set<Customer>().AsNoTracking().Where(c => c.UserId == request.UserId).Select(c => (long?)c.Id).FirstOrDefaultAsync(ct);
        if (customerId is null)
        {
            return Error.Forbidden("Order.NotCustomer", "This area is for customers.");
        }

        if (request.Lines.Count == 0)
        {
            return OrderErrors.EmptyBasket;
        }

        var fulfilment = request.Fulfilment ?? string.Empty;
        if (fulfilment is not (Fulfilments.Delivery or Fulfilments.Pickup))
        {
            return OrderErrors.Field("fulfilment", "Choose delivery or pickup.");
        }

        var lines = new List<OrderLine>();
        var rentalPending = new List<(OrderLine Line, RentalQuoteDto Quote, RentalItem Item)>();

        // ---- shop lines: re-priced from the catalogue ----
        var shopInputs = request.Lines.Where(l => l.Kind == OrderKinds.Shop).ToList();
        var variantIds = shopInputs.Select(l => l.VariantId ?? 0).Distinct().ToList();
        var variants = await db.Set<ProductVariant>().AsNoTracking().Where(v => variantIds.Contains(v.Id) && v.IsActive).ToDictionaryAsync(v => v.Id, ct);
        var productIds = shopInputs.Select(l => l.ProductId ?? 0).Distinct().ToList();
        var products = await db.Set<Product>().AsNoTracking().Where(p => productIds.Contains(p.Id) && p.Status == ProductStatuses.Published).ToDictionaryAsync(p => p.Id, ct);

        // The same variant twice in the basket counts together against stock.
        var wanted = new Dictionary<long, int>();
        foreach (var input in shopInputs)
        {
            if (input.ProductId is not long productId || input.VariantId is not long variantId
                || !products.TryGetValue(productId, out var product)
                || !variants.TryGetValue(variantId, out var variant) || variant.ProductId != productId)
            {
                return OrderErrors.LineInvalid;
            }

            var quantity = Math.Clamp(input.Quantity, 1, OrderRates.MaxLineQuantity);
            wanted[variantId] = wanted.GetValueOrDefault(variantId) + quantity;
            if (variant.StockOnHand <= 0)
            {
                return OrderErrors.OutOfStock(product.TitleEn);
            }

            if (wanted[variantId] > variant.StockOnHand)
            {
                return OrderErrors.NotEnoughStock(product.TitleEn);
            }

            lines.Add(OrderLine.Shop(product.Id, variant.Id, product.TitleEn, product.TitleAr, variant.Sku, quantity, variant.PriceAmount));
        }

        // ---- rental lines: priced by the quote rules ----
        foreach (var input in request.Lines.Where(l => l.Kind == OrderKinds.Rental))
        {
            if (input.RentalItemId is not long itemId)
            {
                return OrderErrors.LineInvalid;
            }

            var item = await db.Set<RentalItem>().AsNoTracking().FirstOrDefaultAsync(i => i.Id == itemId && i.Status == "published", ct);
            if (item is null)
            {
                return OrderErrors.LineInvalid;
            }

            var quote = await sender.Send(new GetRentalQuoteQuery(itemId, input.Unit, input.From, input.To, Math.Max(1, input.Quantity)), ct);
            if (quote.IsError)
            {
                return quote.Errors;
            }

            if (!quote.Value.IsAvailable)
            {
                return OrderErrors.RentalUnavailable(item.TitleEn);
            }

            var q = quote.Value;
            var line = OrderLine.Rental(
                itemId, item.TitleEn, item.TitleAr, q.Quantity,
                (long)Math.Round(q.Subtotal.Amount / (decimal)q.Units, MidpointRounding.AwayFromZero),
                q.Subtotal.Amount, q.Deposit?.Amount ?? 0);
            lines.Add(line);
            rentalPending.Add((line, q, item));
        }

        // ---- address: only a delivery of shop goods needs one ----
        var anyShop = shopInputs.Count > 0;
        Address? address = null;
        if (anyShop && fulfilment == Fulfilments.Delivery)
        {
            // Someone else's address answers exactly like a missing one.
            address = request.AddressId is long addressId
                ? await db.Set<Address>().AsNoTracking().FirstOrDefaultAsync(a => a.Id == addressId && a.CustomerId == customerId, ct)
                : null;
            if (address is null)
            {
                return OrderErrors.Field("addressId", "Choose a delivery address.");
            }
        }

        // ---- totals ----
        var itemsSubtotal = lines.Sum(l => l.LineTotalAmount);
        var deposits = lines.Where(l => l.Kind == OrderKinds.Rental).Sum(l => l.DepositAmount);
        var shipping = anyShop && fulfilment == Fulfilments.Delivery ? OrderRates.DeliveryFeeAmount : 0;
        var tax = (long)Math.Round(itemsSubtotal * OrderRates.TaxRate, MidpointRounding.AwayFromZero);

        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        var order = Order.Place(
            customerId.Value, fulfilment, notes,
            address?.Recipient, address?.Phone, address?.Line1, address?.Line2, address?.City,
            itemsSubtotal, shipping, tax, deposits, lines);

        // Order, its hires and their links are saved together or not at all.
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await unitOfWork.GetRepository<Order>().AddAsync(order, ct);
            await unitOfWork.SaveChangesAsync(ct);

            foreach (var (line, quote, item) in rentalPending)
            {
                var booking = RentalBooking.Create(
                    item.Id, customerId, order.Id,
                    DateOnly.ParseExact(quote.From, "yyyy-MM-dd"), DateOnly.ParseExact(quote.To, "yyyy-MM-dd"),
                    quote.Unit, quote.Quantity, quote.Units, quote.UnitPrice.Amount, quote.Subtotal.Amount, quote.Deposit?.Amount ?? 0);
                await unitOfWork.GetRepository<RentalBooking>().AddAsync(booking, ct);
                await unitOfWork.SaveChangesAsync(ct);
                line.LinkBooking(booking.Id);
            }

            // Written in the same transaction as the order it describes.
            var customerName = await db.Set<Customer>().AsNoTracking().Where(c => c.Id == customerId).Select(c => c.Name).FirstAsync(ct);
            Notifications.NotificationEvents.OrderPlaced(unitOfWork, customerId.Value, order.Id, order.Reference, customerName);
            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }

        return new PlacedOrderDto(order.Id, order.Reference, order.Status, OrderTotalsDto.From(order), order.PlacedAtUtc);
    }
}

public class CancelMyOrderCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CancelMyOrderCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(CancelMyOrderCommand request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var customerId = await db.Set<Customer>().AsNoTracking().Where(c => c.UserId == request.UserId).Select(c => (long?)c.Id).FirstOrDefaultAsync(ct);
        var order = customerId is null
            ? null
            : await unitOfWork.GetRepository<Order>().GetTableAsTracking().Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.CustomerId == customerId, ct);
        if (order is null)
        {
            return OrderErrors.NotFound;
        }

        var cancelled = order.CancelByCustomer();
        if (cancelled.IsError)
        {
            return cancelled.Errors;
        }

        await OrderSupport.CancelRentalsAsync(unitOfWork, order, ct);
        var customerName = await db.Set<Customer>().AsNoTracking().Where(c => c.Id == customerId).Select(c => c.Name).FirstAsync(ct);
        Notifications.NotificationEvents.OrderCancelledByCustomer(unitOfWork, order.Id, order.Reference, customerName);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}

public class SetOrderStatusCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<SetOrderStatusCommand, Result<AdminOrderDto>>
{
    public async Task<Result<AdminOrderDto>> Handle(SetOrderStatusCommand request, CancellationToken ct)
    {
        var order = await unitOfWork.GetRepository<Order>().GetTableAsTracking().Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);
        if (order is null)
        {
            return OrderErrors.NotFound;
        }

        var previous = order.Status;
        var moved = order.MoveTo(request.Status);
        if (moved.IsError)
        {
            return moved.Errors;
        }

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            // Stock leaves the shelf when staff confirm, and returns when a held order is cancelled.
            if (request.Status == OrderStatuses.Confirmed && !await OrderSupport.MoveStockAsync(unitOfWork, order, -1, ct))
            {
                await unitOfWork.RollbackAsync(ct);
                return OrderErrors.NotEnoughStock("an item in this order");
            }

            if (request.Status == OrderStatuses.Cancelled)
            {
                if (OrderStatuses.StockHeld.Contains(previous))
                {
                    await OrderSupport.MoveStockAsync(unitOfWork, order, +1, ct);
                }

                // Unlike the reference, staff cancelling an order also releases its hires.
                await OrderSupport.CancelRentalsAsync(unitOfWork, order, ct);
            }

            // Tell the customer where their order stands.
            Notifications.NotificationEvents.OrderStatusMoved(unitOfWork, order.CustomerId, order.Reference, request.Status);
            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }

        return await OrderMapper.AdminAsync(unitOfWork, order.Id, ct);
    }
}

internal static class OrderMapper
{
    public static async Task<AdminOrderDto> AdminAsync(IUnitOfWork unitOfWork, long orderId, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var order = await db.Set<Order>().AsNoTracking().Include(o => o.Lines).FirstAsync(o => o.Id == orderId, ct);
        var customer = await db.Set<Customer>().AsNoTracking().Where(c => c.Id == order.CustomerId).FirstOrDefaultAsync(ct);
        var email = customer is null
            ? null
            : await db.Set<JoFit.Domain.Identity.Users.User>().AsNoTracking().Where(u => u.Id == customer.UserId).Select(u => u.Email).FirstOrDefaultAsync(ct);
        return AdminOrderDto.From(order, customer?.Name, email);
    }
}
