using JoFit.Application.Common.Interfaces;
using JoFit.Domain.Rental;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Rental;

public static class UnavailableReasons
{
    public const string InsufficientUnits = "insufficient-units";
    public const string InvalidPeriod = "invalid-period";
    public const string UnsupportedUnit = "unsupported-unit";
}

/// <summary>How a hire is priced. Pure functions, shared by the quote and by checkout.</summary>
public static class RentalPricing
{
    /// <summary>Billable units: a day is each calendar day inclusive, a week is ceil(days / 7) (at least 1), an hour is the quantity.</summary>
    public static int BillableUnits(string unit, DateOnly from, DateOnly to, int quantity)
    {
        var days = to.DayNumber - from.DayNumber + 1;
        return unit switch
        {
            RentalUnits.Day => days,
            RentalUnits.Week => Math.Max(1, (int)Math.Ceiling(days / 7.0)),
            _ => Math.Max(1, quantity)
        };
    }

    /// <summary>
    /// subtotal = rate x units x quantity. Known reference behaviour: for an hourly hire the billable units
    /// already are the quantity, so the quantity counts twice (2 hours x 3 = 6 x 3 = 18 x rate). That is probably a
    /// bug in the reference and is kept here until the client confirms the intended hourly maths.
    /// </summary>
    public static long Subtotal(long rate, int units, int quantity) => rate * units * quantity;
}

public sealed record DayAvailability(string Date, int AvailableUnits);

/// <summary>What is free per day: total units, less those out of service, less those held by live bookings.</summary>
public static class RentalAvailability
{
    public static readonly int MaxWindowDays = 120;

    public static async Task<List<DayAvailability>> DaysAsync(
        IUnitOfWork unitOfWork, RentalItem item, DateOnly from, DateOnly to, CancellationToken ct)
        => (await ForItemsAsync(unitOfWork, [item], from, to, ct))[item.Id];

    /// <summary>Free units per day for many items in one round trip.</summary>
    public static async Task<Dictionary<long, List<DayAvailability>>> ForItemsAsync(
        IUnitOfWork unitOfWork, IReadOnlyList<RentalItem> items, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var ids = items.Select(i => i.Id).ToList();
        var holding = RentalBookingStatuses.Holding;
        var bookings = await unitOfWork.GetRepository<RentalBooking>().GetTableNoTracking()
            .Where(b => ids.Contains(b.RentalItemId) && holding.Contains(b.Status) && b.From <= to && b.To >= from)
            .Select(b => new { b.RentalItemId, b.From, b.To, b.Quantity })
            .ToListAsync(ct);

        var result = new Dictionary<long, List<DayAvailability>>();
        foreach (var item in items)
        {
            var days = new List<DayAvailability>();
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                var held = bookings.Where(b => b.RentalItemId == item.Id && b.From <= day && b.To >= day).Sum(b => b.Quantity);
                days.Add(new DayAvailability(day.ToString("yyyy-MM-dd"), Math.Max(0, item.TotalUnits - item.UnitsOutOfService - held)));
            }

            result[item.Id] = days;
        }

        return result;
    }
}
