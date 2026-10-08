using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Community;

public static class CommunityStatuses
{
    public const string Visible = "visible";
    public const string Flagged = "flagged";
    public const string Hidden = "hidden";
}

public class CommunityTopic : Entity
{
    public string Slug { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;

    private CommunityTopic()
    {
    }

    public static CommunityTopic Create(string slug, string nameEn, string nameAr) => new() { Slug = slug, NameEn = nameEn, NameAr = nameAr };

    public static readonly (string Slug, string En, string Ar)[] Defaults =
    [
        ("training", "Training", "التدريب"),
        ("nutrition", "Nutrition", "التغذية"),
        ("gear", "Gear", "المعدات"),
        ("events", "Events", "الفعاليات"),
        ("general", "General", "عام")
    ];
}

/// <summary>A member's post. Hidden posts are not public; flagged ones stay public until staff decide.</summary>
public class CommunityPost : AuditableEntity
{
    public long TopicId { get; private set; }
    public long AuthorCustomerId { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Excerpt { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string Status { get; private set; } = CommunityStatuses.Visible;
    public string? ModerationNote { get; private set; }
    public int ReportCount { get; private set; }
    public int LikeCount { get; private set; }
    public int CommentCount { get; private set; }

    private CommunityPost()
    {
    }

    public void LowerCommentCount(int by) => CommentCount = Math.Max(0, CommentCount - by);

    /// <summary>Visible and flagged can become visible or hidden. Hiding needs a note; making it visible resets the reports.</summary>
    public Result<Updated> Moderate(string status, string? note)
    {
        if (status is not (CommunityStatuses.Visible or CommunityStatuses.Hidden))
        {
            return CommunityErrors.Field("status", "Choose visible or hidden.");
        }

        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (status == CommunityStatuses.Hidden && trimmed is null)
        {
            return CommunityErrors.Field("note", "Say why it is hidden.");
        }

        Status = status;
        ModerationNote = status == CommunityStatuses.Hidden ? trimmed : null;
        if (status == CommunityStatuses.Visible)
        {
            ReportCount = 0;
        }

        return Result.Updated;
    }
}

public class CommunityComment : AuditableEntity
{
    public long PostId { get; private set; }
    public long? ParentCommentId { get; private set; }
    public long AuthorCustomerId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public string Status { get; private set; } = CommunityStatuses.Visible;
    public string? ModerationNote { get; private set; }
    public int ReportCount { get; private set; }

    private CommunityComment()
    {
    }

    public Result<Updated> Moderate(string status, string? note)
    {
        if (status is not (CommunityStatuses.Visible or CommunityStatuses.Hidden))
        {
            return CommunityErrors.Field("status", "Choose visible or hidden.");
        }

        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (status == CommunityStatuses.Hidden && trimmed is null)
        {
            return CommunityErrors.Field("note", "Say why it is hidden.");
        }

        Status = status;
        ModerationNote = status == CommunityStatuses.Hidden ? trimmed : null;
        if (status == CommunityStatuses.Visible)
        {
            ReportCount = 0;
        }

        return Result.Updated;
    }
}

public static class CommunityErrors
{
    public static readonly Error PostNotFound = Error.NotFound("Community.PostNotFound", "That post does not exist.");
    public static readonly Error CommentNotFound = Error.NotFound("Community.CommentNotFound", "That comment does not exist.");

    public static Error Field(string field, string message)
        => Error.Validation($"Community.{field}", message).ForField(field);
}
