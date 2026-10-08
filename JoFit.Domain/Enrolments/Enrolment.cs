using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Enrolments;

public static class EnrolmentStatuses
{
    public const string Pending = "pending";
    public const string Active = "active";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";

    public static readonly string[] All = [Pending, Active, Completed, Cancelled];

    /// <summary>A pending enrolment holds a seat until payment, so pending and active both count as taken.</summary>
    public static readonly string[] Live = [Pending, Active];

    public static bool CanMove(string from, string to) => (from, to) switch
    {
        (Pending, Active) or (Pending, Cancelled) => true,
        (Active, Completed) or (Active, Cancelled) => true,
        _ => false
    };
}

public class Enrolment : AuditableEntity
{
    public long CourseId { get; private set; }
    public long CustomerId { get; private set; }
    public string Status { get; private set; } = EnrolmentStatuses.Pending;

    /// <summary>The intake start date this enrolment is for (the course's next intake when they enrolled).</summary>
    public DateOnly? CohortStartsOn { get; private set; }
    public long AmountPaidAmount { get; private set; }
    public int ProgressPercent { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset EnrolledAtUtc { get; private set; }

    private Enrolment()
    {
    }

    /// <summary>EN-00042, derived from the id.</summary>
    public string Reference => $"EN-{Id:D5}";

    public static Enrolment Reserve(long courseId, long customerId, DateOnly? cohortStartsOn)
        => new()
        {
            CourseId = courseId,
            CustomerId = customerId,
            CohortStartsOn = cohortStartsOn,
            EnrolledAtUtc = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Pending to active or cancelled, active to completed or cancelled; cancelling needs a note.
    /// Activating records the course price as paid; completing sets progress to 100.
    /// </summary>
    public Result<Updated> MoveTo(string status, string? note, long coursePriceAmount)
    {
        if (!EnrolmentStatuses.All.Contains(status))
        {
            return EnrolmentErrors.Field("status", "Unknown status.");
        }

        if (!EnrolmentStatuses.CanMove(Status, status))
        {
            return EnrolmentErrors.IllegalMove(Status, status);
        }

        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (status == EnrolmentStatuses.Cancelled && trimmed is null)
        {
            return EnrolmentErrors.Field("note", "Say why the enrolment is cancelled.");
        }

        if (status == EnrolmentStatuses.Active)
        {
            AmountPaidAmount = coursePriceAmount;
        }

        if (status == EnrolmentStatuses.Completed)
        {
            ProgressPercent = 100;
        }

        if (trimmed is not null)
        {
            Note = trimmed;
        }

        Status = status;
        return Result.Updated;
    }
}

public static class EnrolmentErrors
{
    public static readonly Error NotFound = Error.NotFound("Enrolment.NotFound", "That enrolment does not exist.");
    public static readonly Error AlreadyEnrolled = Error.Conflict("Enrolment.AlreadyEnrolled", "You are already enrolled in this course.");
    public static readonly Error Full = Error.Conflict("Enrolment.Full", "The next intake is full.");

    public static Error Field(string field, string message)
        => Error.Validation($"Enrolment.{field}", message).ForField(field);

    public static Error IllegalMove(string from, string to)
        => Error.Conflict("Enrolment.IllegalMove", $"A {from} enrolment cannot become {to}.");
}
