using System.Text.RegularExpressions;
using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Quotations;

public static class QuotationStatuses
{
    public const string New = "new";
    public const string InReview = "in-review";
    public const string Quoted = "quoted";
    public const string Won = "won";
    public const string Lost = "lost";

    public static readonly string[] All = [New, InReview, Quoted, Won, Lost];
}

/// <summary>What a quotation is for. The storefront lists these in the requested language.</summary>
public static class QuotationSubjects
{
    public static readonly (string Kind, string En, string Ar)[] All =
    [
        ("team-kit", "Team kit and uniforms", "ملابس وأطقم الفرق"),
        ("gym-equipment", "Gym and fitness equipment", "معدات النادي واللياقة"),
        ("facility", "Sports facility supplies", "مستلزمات المنشآت الرياضية"),
        ("coaching-programme", "Coaching programme", "برنامج تدريب"),
        ("education", "Courses and certification", "دورات وشهادات"),
        ("event", "Event or tournament", "فعالية أو بطولة"),
        ("other", "Something else", "أمر آخر")
    ];

    public static bool IsKnown(string? kind) => All.Any(s => s.Kind == kind);
}

/// <summary>A business asking for a price. Always filed as a guest request today.</summary>
public class QuotationRequest : AuditableEntity
{
    private static readonly Regex EmailPattern = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"^\+?[\d\s]{8,}$", RegexOptions.Compiled);

    public string SubjectKind { get; private set; } = string.Empty;
    public string OrganisationName { get; private set; } = string.Empty;
    public string ContactName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string PreferredContact { get; private set; } = "email";
    public string Message { get; private set; } = string.Empty;
    public string? QuantityEstimate { get; private set; }
    public string? BudgetRange { get; private set; }
    public long? CustomerId { get; private set; }
    public string Status { get; private set; } = QuotationStatuses.New;

    /// <summary>The staff member (identity user id) who owns it.</summary>
    public long? AssigneeId { get; private set; }
    public string? InternalNote { get; private set; }
    public DateTimeOffset SubmittedAtUtc { get; private set; }

    private QuotationRequest()
    {
    }

    /// <summary>QR-00042, derived from the id.</summary>
    public string Reference => $"QR-{Id:D5}";

    public static Result<QuotationRequest> Submit(
        string? subjectKind, string? organisation, string? contactName, string? email, string? phone, string? preferredContact,
        string? message, string? quantityEstimate, string? budgetRange, long? customerId)
    {
        var errors = new List<Error>();
        var org = (organisation ?? string.Empty).Trim();
        var contact = (contactName ?? string.Empty).Trim();
        var mail = (email ?? string.Empty).Trim();
        var phoneValue = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        var text = (message ?? string.Empty).Trim();
        var preferred = preferredContact == "phone" ? "phone" : "email";

        if (!QuotationSubjects.IsKnown(subjectKind)) errors.Add(Field("subjectKind", "Choose what the quotation is for."));
        if (org.Length < 2) errors.Add(Field("organisationName", "Tell us the organisation's name."));
        if (contact.Length < 2) errors.Add(Field("contactName", "Tell us who to contact."));
        if (!EmailPattern.IsMatch(mail)) errors.Add(Field("email", "An email address we can reach you on."));
        if (preferred == "phone" && (phoneValue is null || !PhonePattern.IsMatch(phoneValue))) errors.Add(Field("phone", "Give a phone number (at least 8 digits) so we can call you."));
        if (phoneValue is not null && !PhonePattern.IsMatch(phoneValue)) errors.Add(Field("phone", "A phone number with at least 8 digits."));
        if (text.Length < 20) errors.Add(Field("message", "Tell us a little more about what you need (at least 20 characters)."));
        if (errors.Count > 0)
        {
            return errors;
        }

        return new QuotationRequest
        {
            SubjectKind = subjectKind!,
            OrganisationName = org,
            ContactName = contact,
            Email = mail.ToLowerInvariant(),
            Phone = phoneValue,
            PreferredContact = preferred,
            Message = text,
            QuantityEstimate = string.IsNullOrWhiteSpace(quantityEstimate) ? null : quantityEstimate.Trim(),
            BudgetRange = string.IsNullOrWhiteSpace(budgetRange) ? null : budgetRange.Trim(),
            CustomerId = customerId,
            SubmittedAtUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Assigning someone to a new request moves it to in-review. Clearing the assignee never moves it back.</summary>
    public void Assign(long? assigneeId)
    {
        AssigneeId = assigneeId;
        if (assigneeId is not null && Status == QuotationStatuses.New)
        {
            Status = QuotationStatuses.InReview;
        }
    }

    /// <summary>Any move is allowed. Marking it lost needs an internal note.</summary>
    public Result<Updated> SetStatus(string status, string? internalNote)
    {
        if (!QuotationStatuses.All.Contains(status))
        {
            return Field("status", "Choose a status from the list.");
        }

        var note = string.IsNullOrWhiteSpace(internalNote) ? null : internalNote.Trim();
        if (status == QuotationStatuses.Lost && note is null)
        {
            return Field("internalNote", "Say why it was lost, for the team's records.");
        }

        Status = status;
        if (note is not null)
        {
            InternalNote = note;
        }

        return Result.Updated;
    }

    private static Error Field(string field, string message) => Error.Validation($"Quotation.{field}", message).ForField(field);
}

public static class QuotationErrors
{
    public static readonly Error NotFound = Error.NotFound("Quotation.NotFound", "That quotation request does not exist.");
    public static readonly Error AssigneeMissing = Error.Validation("Quotation.AssigneeMissing", "That staff member does not exist.").ForField("assigneeId");
}
