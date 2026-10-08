using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Support;

public static class SupportStatuses
{
    public const string Open = "open";
    public const string Closed = "closed";
}

public static class SupportSenders
{
    public const string Visitor = "visitor";
    public const string Staff = "staff";
}

/// <summary>A chat between a visitor (signed in or guest) and the staff team.</summary>
public class SupportConversation : AuditableEntity
{
    /// <summary>The signed-in visitor's identity user, when there is one.</summary>
    public long? AccountUserId { get; private set; }
    public long? CustomerId { get; private set; }

    /// <summary>SHA-256 of the guest token. A guest proves it is them by sending the token back.</summary>
    public string? GuestTokenHash { get; private set; }
    public string VisitorName { get; private set; } = string.Empty;
    public string VisitorEmail { get; private set; } = string.Empty;
    public string Status { get; private set; } = SupportStatuses.Open;
    public long? AssigneeId { get; private set; }
    public DateTimeOffset LastMessageAtUtc { get; private set; }
    public DateTimeOffset? VisitorReadAtUtc { get; private set; }
    public DateTimeOffset? StaffReadAtUtc { get; private set; }

    public List<SupportMessage> Messages { get; private set; } = [];

    private SupportConversation()
    {
    }

    /// <summary>SC-00042, derived from the id.</summary>
    public string Reference => $"SC-{Id:D5}";

    public static SupportConversation Start(long? accountUserId, long? customerId, string? guestTokenHash, string name, string email)
        => new()
        {
            AccountUserId = accountUserId,
            CustomerId = customerId,
            GuestTokenHash = guestTokenHash,
            VisitorName = name,
            VisitorEmail = email,
            LastMessageAtUtc = DateTimeOffset.UtcNow
        };

    public SupportMessage Add(string sender, long? staffUserId, string authorName, string body)
    {
        var message = SupportMessage.Create(sender, staffUserId, authorName, body);
        Messages.Add(message);
        LastMessageAtUtc = message.SentAtUtc;
        return message;
    }

    public void MarkReadByVisitor() => VisitorReadAtUtc = DateTimeOffset.UtcNow;

    public void MarkReadByStaff() => StaffReadAtUtc = DateTimeOffset.UtcNow;

    public void SetStatus(string status) => Status = status == SupportStatuses.Closed ? SupportStatuses.Closed : SupportStatuses.Open;
}

public class SupportMessage : Entity
{
    public long ConversationId { get; private set; }
    public string Sender { get; private set; } = SupportSenders.Visitor;
    public long? StaffUserId { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public DateTimeOffset SentAtUtc { get; private set; }

    private SupportMessage()
    {
    }

    public static SupportMessage Create(string sender, long? staffUserId, string authorName, string body)
        => new() { Sender = sender, StaffUserId = staffUserId, AuthorName = authorName, Body = body, SentAtUtc = DateTimeOffset.UtcNow };
}

public static class SupportErrors
{
    public static readonly Error NotFound = Error.NotFound("Support.NotFound", "That conversation does not exist.");

    public static Error Field(string field, string message)
        => Error.Validation($"Support.{field}", message).ForField(field);

    public static readonly Error BodyLength = Field("body", "Write between 2 and 2000 characters.");
    public static readonly Error ReplyLength = Field("body", "Write between 1 and 2000 characters.");
    public static readonly Error GuestName = Field("name", "Tell us your name (at least 2 characters).");
    public static readonly Error GuestEmail = Field("email", "An email address we can reply to.");
}
