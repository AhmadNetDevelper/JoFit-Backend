using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Courses.Catalogue;
using JoFit.Domain.Orders;

namespace JoFit.Application.Features.Orders;

public sealed record OrderTotalsDto(MoneyDto ItemsSubtotal, MoneyDto Shipping, MoneyDto Tax, MoneyDto DepositsTotal, MoneyDto GrandTotal)
{
    public static OrderTotalsDto From(Order o)
        => new(new MoneyDto(o.ItemsSubtotalAmount), new MoneyDto(o.ShippingAmount), new MoneyDto(o.TaxAmount), new MoneyDto(o.DepositsTotalAmount), new MoneyDto(o.GrandTotalAmount));
}

public sealed record OrderAddressDto(string Recipient, string Phone, string Line1, string? Line2, string City);

public sealed record AdminOrderLineDto(
    long Id, string Kind, LocalizedText Title, string? Sku, int Quantity, MoneyDto UnitPrice, MoneyDto LineTotal, MoneyDto? Deposit,
    long? ProductId, long? VariantId, long? RentalItemId, long? RentalBookingId)
{
    public static AdminOrderLineDto From(OrderLine l)
        => new(l.Id, l.Kind, new LocalizedText(l.TitleEn, l.TitleAr), l.Sku, l.Quantity, new MoneyDto(l.UnitPriceAmount), new MoneyDto(l.LineTotalAmount),
            l.DepositAmount > 0 ? new MoneyDto(l.DepositAmount) : null, l.ProductId, l.VariantId, l.RentalItemId, l.RentalBookingId);
}

public sealed record AdminOrderDto(
    long Id,
    string Reference,
    long CustomerId,
    string? CustomerName,
    string? CustomerEmail,
    string Status,
    IReadOnlyList<string> NextStatuses,
    string Fulfilment,
    OrderAddressDto? Address,
    string? Notes,
    IReadOnlyList<AdminOrderLineDto> Lines,
    OrderTotalsDto Totals,
    DateTimeOffset PlacedAt,
    AuditDto Audit)
{
    public static OrderAddressDto? AddressOf(Order o)
        => o.ShipLine1 is null ? null : new OrderAddressDto(o.ShipRecipient ?? string.Empty, o.ShipPhone ?? string.Empty, o.ShipLine1, o.ShipLine2, o.ShipCity ?? string.Empty);

    public static AdminOrderDto From(Order o, string? customerName, string? customerEmail)
        => new(
            o.Id, o.Reference, o.CustomerId, customerName, customerEmail, o.Status, OrderStatuses.NextOf(o.Status), o.Fulfilment,
            AddressOf(o), o.Notes, o.Lines.Select(AdminOrderLineDto.From).ToList(), OrderTotalsDto.From(o), o.PlacedAtUtc, AuditDto.From(o));
}

public sealed record AccountOrderLineDto(
    long Id, string Kind, string Title, string? Sku, int Quantity, MoneyDto UnitPrice, MoneyDto LineTotal, MoneyDto? Deposit)
{
    public static AccountOrderLineDto From(OrderLine l, ILocalizationService loc, string? language)
        => new(l.Id, l.Kind, loc.GetLocalizedName(l.TitleAr, l.TitleEn, language), l.Sku, l.Quantity, new MoneyDto(l.UnitPriceAmount),
            new MoneyDto(l.LineTotalAmount), l.DepositAmount > 0 ? new MoneyDto(l.DepositAmount) : null);
}

public sealed record OrderProgressStep(string Status, bool Done);

public sealed record AccountOrderDto(
    long Id,
    string Reference,
    string Status,
    string Fulfilment,
    OrderAddressDto? Address,
    string? Notes,
    IReadOnlyList<AccountOrderLineDto> Lines,
    OrderTotalsDto Totals,
    DateTimeOffset PlacedAt,
    IReadOnlyList<OrderProgressStep> Progress,
    bool CanCancel)
{
    private static readonly string[] Steps = [OrderStatuses.Pending, OrderStatuses.Confirmed, OrderStatuses.Processing, OrderStatuses.Shipped, OrderStatuses.Completed];

    /// <summary>The five steps with "done" up to the current status. Cancelled and refunded orders have none.</summary>
    public static IReadOnlyList<OrderProgressStep> ProgressOf(string status)
    {
        var index = Array.IndexOf(Steps, status);
        return index < 0 ? [] : Steps.Select((s, i) => new OrderProgressStep(s, i <= index)).ToList();
    }

    public static AccountOrderDto From(Order o, ILocalizationService loc, string? language)
        => new(
            o.Id, o.Reference, o.Status, o.Fulfilment, AdminOrderDto.AddressOf(o), o.Notes,
            o.Lines.Select(l => AccountOrderLineDto.From(l, loc, language)).ToList(), OrderTotalsDto.From(o), o.PlacedAtUtc,
            ProgressOf(o.Status), o.Status == OrderStatuses.Pending);
}

public sealed record PlacedOrderDto(long Id, string Reference, string Status, OrderTotalsDto Totals, DateTimeOffset PlacedAt);
