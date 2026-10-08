using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Rental;

public static class RentalBookingStatuses
{
    public const string Pending = "pending";
    public const string Confirmed = "confirmed";
    public const string Collected = "collected";
    public const string Returned = "returned";
    public const string Cancelled = "cancelled";

    public static readonly string[] All = [Pending, Confirmed, Collected, Returned, Cancelled];

    /// <summary>Statuses whose units are held and so are unavailable to others.</summary>
    public static readonly string[] Holding = [Pending, Confirmed, Collected];

    private static readonly Dictionary<string, string[]> Moves = new()
    {
        [Pending] = [Confirmed, Cancelled],
        [Confirmed] = [Collected, Cancelled],
        [Collected] = [Returned]
    };

    public static bool CanMove(string from, string to) => Moves.TryGetValue(from, out var next) && next.Contains(to);
}

/// <summary>A hire of one rental item over a period. Created by checkout (with the order) and moved by staff.</summary>
public class RentalBooking : AuditableEntity
{
    public long RentalItemId { get; private set; }
    public long? CustomerId { get; private set; }

    /// <summary>The order this hire belongs to, once checkout exists.</summary>
    public long? OrderId { get; private set; }
    public DateOnly From { get; private set; }
    public DateOnly To { get; private set; }
    public string Unit { get; private set; } = RentalUnits.Day;
    public int Quantity { get; private set; }

    /// <summary>Billable units (days, weeks or hours) for the period.</summary>
    public int Units { get; private set; }
    public long UnitPriceAmount { get; private set; }
    public long SubtotalAmount { get; private set; }
    public long DepositAmount { get; private set; }
    public string Status { get; private set; } = RentalBookingStatuses.Pending;
    public DateTimeOffset? DepositReleasedAtUtc { get; private set; }

    private RentalBooking()
    {
    }

    /// <summary>RB-00042. Derived from the id, so it never needs its own column.</summary>
    public string Reference => $"RB-{Id:D5}";

    public static RentalBooking Create(
        long itemId, long? customerId, long? orderId, DateOnly from, DateOnly to, string unit, int quantity, int units,
        long unitPriceAmount, long subtotalAmount, long depositAmount)
        => new()
        {
            RentalItemId = itemId,
            CustomerId = customerId,
            OrderId = orderId,
            From = from,
            To = to,
            Unit = unit,
            Quantity = quantity,
            Units = units,
            UnitPriceAmount = unitPriceAmount,
            SubtotalAmount = subtotalAmount,
            DepositAmount = depositAmount
        };

    public Result<Updated> MoveTo(string status)
    {
        if (!RentalBookingStatuses.All.Contains(status))
        {
            return RentalErrors.Field("status", "Unknown status.");
        }

        if (!RentalBookingStatuses.CanMove(Status, status))
        {
            return RentalErrors.IllegalMove(Status, status);
        }

        Status = status;
        return Result.Updated;
    }

    /// <summary>Releases the units when the order they belong to is cancelled. A hire already returned stays as it is.</summary>
    public void CancelWithOrder()
    {
        if (Status is not (RentalBookingStatuses.Returned or RentalBookingStatuses.Cancelled))
        {
            Status = RentalBookingStatuses.Cancelled;
        }
    }

    /// <summary>Only once the goods are back, and only when a deposit was taken.</summary>
    public Result<Updated> ReleaseDeposit()
    {
        if (DepositAmount <= 0)
        {
            return RentalErrors.Field("deposit", "This hire has no deposit to release.");
        }

        if (Status != RentalBookingStatuses.Returned)
        {
            return RentalErrors.DepositNotReleasable;
        }

        if (DepositReleasedAtUtc is not null)
        {
            return RentalErrors.DepositAlreadyReleased;
        }

        DepositReleasedAtUtc = DateTimeOffset.UtcNow;
        return Result.Updated;
    }
}
