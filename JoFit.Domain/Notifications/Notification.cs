using JoFit.Domain.Common;

namespace JoFit.Domain.Notifications;

public static class NotificationAudiences
{
    public const string Customer = "customer";
    public const string Coach = "coach";
    public const string Employer = "employer";

    /// <summary>The whole back-office team. There is no recipient; one person reading it clears it for everyone.</summary>
    public const string Staff = "staff";
}

public static class NotificationKinds
{
    public const string OrderPlaced = "order-placed";
    public const string OrderStatus = "order-status";
    public const string RentalStatus = "rental-status";
    public const string SessionRequested = "session-requested";
    public const string SessionStatus = "session-status";
    public const string EnrolmentRequested = "enrolment-requested";
    public const string EnrolmentStatus = "enrolment-status";
    public const string ReviewSubmitted = "review-submitted";
    public const string ReviewModerated = "review-moderated";
    public const string QuotationReceived = "quotation-received";
    public const string SupportMessage = "support-message";
    public const string EmployerRegistered = "employer-registered";
    public const string EmployerVerification = "employer-verification";
    public const string JobSubmitted = "job-submitted";
    public const string JobModerated = "job-moderated";
    public const string JobApplication = "job-application";
    public const string ApplicationStatus = "application-status";

    public static readonly string[] All =
    [
        OrderPlaced, OrderStatus, RentalStatus, SessionRequested, SessionStatus, EnrolmentRequested, EnrolmentStatus,
        ReviewSubmitted, ReviewModerated, QuotationReceived, SupportMessage, EmployerRegistered, EmployerVerification,
        JobSubmitted, JobModerated, JobApplication, ApplicationStatus
    ];
}

/// <summary>One in-site notification: an event written in both languages, for a person or for the staff team.</summary>
public class Notification : Entity
{
    public string Audience { get; private set; } = string.Empty;
    public long? RecipientId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public string TitleEn { get; private set; } = string.Empty;
    public string TitleAr { get; private set; } = string.Empty;
    public string BodyEn { get; private set; } = string.Empty;
    public string BodyAr { get; private set; } = string.Empty;

    /// <summary>A path without the locale prefix, for example /account/orders.</summary>
    public string Href { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }

    private Notification()
    {
    }

    public static Notification Create(
        string audience, long? recipientId, string kind, string titleEn, string titleAr, string bodyEn, string bodyAr, string href)
        => new()
        {
            Audience = audience,
            RecipientId = recipientId,
            Kind = kind,
            TitleEn = titleEn,
            TitleAr = titleAr,
            BodyEn = bodyEn,
            BodyAr = bodyAr,
            Href = href,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

    public void MarkRead() => ReadAtUtc ??= DateTimeOffset.UtcNow;
}
