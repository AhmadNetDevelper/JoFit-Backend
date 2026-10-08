using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Coaches;

public static class CoachBookingStatuses
{
    public const string Pending = "pending";
    public const string Confirmed = "confirmed";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";

    public static readonly string[] All = [Pending, Confirmed, Completed, Cancelled];

    /// <summary>A booking in these statuses holds its time slot.</summary>
    public static readonly string[] Live = [Pending, Confirmed, Completed];

    public static bool CanMove(string from, string to) => (from, to) switch
    {
        (Pending, Confirmed) or (Pending, Cancelled) => true,
        (Confirmed, Completed) or (Confirmed, Cancelled) => true,
        _ => false
    };
}

/// <summary>A session with a coach. A guest's booking has no customer and still reaches the coach.</summary>
public class CoachBooking : AuditableEntity
{
    public long CoachId { get; private set; }
    public long PackageId { get; private set; }
    public long? CustomerId { get; private set; }
    public string ContactName { get; private set; } = string.Empty;
    public string ContactEmail { get; private set; } = string.Empty;
    public string? ContactPhone { get; private set; }
    public string? Goal { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public string Status { get; private set; } = CoachBookingStatuses.Pending;

    /// <summary>The reason given when a session is cancelled.</summary>
    public string? Note { get; private set; }
    public long TotalAmount { get; private set; }

    private CoachBooking()
    {
    }

    /// <summary>CB-00042, derived from the id.</summary>
    public string Reference => $"CB-{Id:D5}";

    public static CoachBooking Create(
        long coachId, long packageId, long? customerId, string contactName, string contactEmail, string? contactPhone, string? goal,
        DateTimeOffset startsAt, DateTimeOffset endsAt, long totalAmount)
        => new()
        {
            CoachId = coachId,
            PackageId = packageId,
            CustomerId = customerId,
            ContactName = contactName,
            ContactEmail = contactEmail,
            ContactPhone = contactPhone,
            Goal = goal,
            StartsAt = startsAt,
            EndsAt = endsAt,
            TotalAmount = totalAmount
        };

    /// <summary>
    /// Moves the status along pending to confirmed or cancelled, confirmed to completed or cancelled. Cancelling needs a note.
    /// Only a coach is held to "completed after the start time"; staff are not time-gated, so that check lives with the caller.
    /// </summary>
    public Result<Updated> MoveTo(string status, string? note)
    {
        if (!CoachBookingStatuses.All.Contains(status))
        {
            return CoachErrors.Field("status", "Unknown status.");
        }

        if (!CoachBookingStatuses.CanMove(Status, status))
        {
            return CoachErrors.IllegalMove(Status, status);
        }

        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (status == CoachBookingStatuses.Cancelled && (trimmed is null || trimmed.Length < 3))
        {
            return CoachErrors.Field("note", "Say why the session is cancelled (at least 3 characters).");
        }

        Status = status;
        if (status == CoachBookingStatuses.Cancelled)
        {
            Note = trimmed;
        }

        return Result.Updated;
    }
}
