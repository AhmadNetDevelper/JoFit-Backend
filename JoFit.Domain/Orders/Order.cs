using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Orders;

public static class OrderStatuses
{
    public const string Pending = "pending";
    public const string Confirmed = "confirmed";
    public const string Processing = "processing";
    public const string Shipped = "shipped";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
    public const string Refunded = "refunded";

    public static readonly string[] All = [Pending, Confirmed, Processing, Shipped, Completed, Cancelled, Refunded];

    /// <summary>The stock of a line is held from confirmation until the order is cancelled or done.</summary>
    public static readonly string[] StockHeld = [Confirmed, Processing, Shipped, Completed];

    private static readonly Dictionary<string, string[]> Moves = new()
    {
        [Pending] = [Confirmed, Cancelled],
        [Confirmed] = [Processing, Cancelled],
        [Processing] = [Shipped, Cancelled],
        [Shipped] = [Completed],
        [Completed] = [Refunded]
    };

    public static bool CanMove(string from, string to) => Moves.TryGetValue(from, out var next) && next.Contains(to);

    public static IReadOnlyList<string> NextOf(string from) => Moves.TryGetValue(from, out var next) ? next : [];
}

public static class OrderKinds
{
    public const string Shop = "shop";
    public const string Rental = "rental";
}

public static class Fulfilments
{
    public const string Delivery = "delivery";
    public const string Pickup = "pickup";
}

/// <summary>A reservation paid on collection or delivery. Created pending; staff confirm it.</summary>
public class Order : AuditableEntity
{
    public long CustomerId { get; private set; }
    public string Status { get; private set; } = OrderStatuses.Pending;
    public string Fulfilment { get; private set; } = Fulfilments.Pickup;

    // The delivery address as it was when the order was placed.
    public string? ShipRecipient { get; private set; }
    public string? ShipPhone { get; private set; }
    public string? ShipLine1 { get; private set; }
    public string? ShipLine2 { get; private set; }
    public string? ShipCity { get; private set; }
    public string? Notes { get; private set; }

    public long ItemsSubtotalAmount { get; private set; }
    public long ShippingAmount { get; private set; }
    public long TaxAmount { get; private set; }
    public long DepositsTotalAmount { get; private set; }
    public long GrandTotalAmount { get; private set; }
    public DateTimeOffset PlacedAtUtc { get; private set; }

    public List<OrderLine> Lines { get; private set; } = [];

    private Order()
    {
    }

    /// <summary>JF-00042, derived from the id.</summary>
    public string Reference => $"JF-{Id:D5}";

    public static Order Place(
        long customerId, string fulfilment, string? notes,
        string? recipient, string? phone, string? line1, string? line2, string? city,
        long itemsSubtotal, long shipping, long tax, long deposits, IEnumerable<OrderLine> lines)
    {
        var order = new Order
        {
            CustomerId = customerId,
            Fulfilment = fulfilment,
            Notes = notes,
            ShipRecipient = recipient,
            ShipPhone = phone,
            ShipLine1 = line1,
            ShipLine2 = line2,
            ShipCity = city,
            ItemsSubtotalAmount = itemsSubtotal,
            ShippingAmount = shipping,
            TaxAmount = tax,
            DepositsTotalAmount = deposits,
            GrandTotalAmount = itemsSubtotal + deposits + shipping + tax,
            PlacedAtUtc = DateTimeOffset.UtcNow
        };
        order.Lines.AddRange(lines);
        return order;
    }

    public Result<Updated> MoveTo(string status)
    {
        if (!OrderStatuses.All.Contains(status))
        {
            return OrderErrors.Field("status", "Unknown status.");
        }

        if (!OrderStatuses.CanMove(Status, status))
        {
            return OrderErrors.IllegalMove(Status, status);
        }

        Status = status;
        return Result.Updated;
    }

    /// <summary>A customer may cancel only while the order is still pending.</summary>
    public Result<Updated> CancelByCustomer()
        => Status == OrderStatuses.Pending ? MoveTo(OrderStatuses.Cancelled) : OrderErrors.NotCancellable;
}

public class OrderLine : Entity
{
    public long OrderId { get; private set; }
    public string Kind { get; private set; } = OrderKinds.Shop;
    public long? ProductId { get; private set; }
    public long? VariantId { get; private set; }
    public long? RentalItemId { get; private set; }
    public long? RentalBookingId { get; private set; }
    public string TitleEn { get; private set; } = string.Empty;
    public string TitleAr { get; private set; } = string.Empty;
    public string? Sku { get; private set; }
    public int Quantity { get; private set; }
    public long UnitPriceAmount { get; private set; }
    public long LineTotalAmount { get; private set; }
    public long DepositAmount { get; private set; }

    private OrderLine()
    {
    }

    public static OrderLine Shop(long productId, long variantId, string titleEn, string titleAr, string sku, int quantity, long unitPrice)
        => new()
        {
            Kind = OrderKinds.Shop,
            ProductId = productId,
            VariantId = variantId,
            TitleEn = titleEn,
            TitleAr = titleAr,
            Sku = sku,
            Quantity = quantity,
            UnitPriceAmount = unitPrice,
            LineTotalAmount = unitPrice * quantity
        };

    public static OrderLine Rental(long itemId, string titleEn, string titleAr, int quantity, long unitPrice, long lineTotal, long deposit)
        => new()
        {
            Kind = OrderKinds.Rental,
            RentalItemId = itemId,
            TitleEn = titleEn,
            TitleAr = titleAr,
            Quantity = quantity,
            UnitPriceAmount = unitPrice,
            LineTotalAmount = lineTotal,
            DepositAmount = deposit
        };

    public void LinkBooking(long bookingId) => RentalBookingId = bookingId;
}
