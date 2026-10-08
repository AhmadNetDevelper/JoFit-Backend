using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Reviews;

public static class ReviewTargets
{
    public const string Product = "product";
    public const string RentalItem = "rental-item";
    public const string Coach = "coach";
    public const string Course = "course";

    public static readonly string[] All = [Product, RentalItem, Coach, Course];
}

public static class ReviewModeration
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}

/// <summary>A rating and text a customer left for something they bought, hired, took or attended.</summary>
public class Review : AuditableEntity
{
    public long CustomerId { get; private set; }
    public string TargetType { get; private set; } = string.Empty;
    public long TargetId { get; private set; }
    public int Rating { get; private set; }
    public string? Title { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public string ModerationStatus { get; private set; } = ReviewModeration.Pending;
    public string? ModerationNote { get; private set; }
    public int HelpfulCount { get; private set; }
    public DateTimeOffset SubmittedAtUtc { get; private set; }

    private Review()
    {
    }

    /// <summary>A new review starts pending and is not public until staff approve it.</summary>
    public static Result<Review> Submit(long customerId, string? targetType, long targetId, int rating, string? title, string? body)
    {
        var errors = new List<Error>();
        var text = (body ?? string.Empty).Trim();
        if (rating is < 1 or > 5) errors.Add(ReviewErrors.Field("rating", "Choose a rating from 1 to 5."));
        if (text.Length < 10) errors.Add(ReviewErrors.Field("body", "Tell us a little more (at least 10 characters)."));
        if (text.Length > 3000) errors.Add(ReviewErrors.Field("body", "Keep it under 3000 characters."));
        var cleanTitle = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        if (cleanTitle is { Length: > 120 }) errors.Add(ReviewErrors.Field("title", "The title cannot exceed 120 characters."));
        if (errors.Count > 0)
        {
            return errors;
        }

        return new Review
        {
            CustomerId = customerId,
            TargetType = targetType ?? string.Empty,
            TargetId = targetId,
            Rating = rating,
            Title = cleanTitle,
            Body = text,
            SubmittedAtUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Staff approve or reject (any move between the two is allowed, but pending can never be set). Rejecting needs a note.
    /// </summary>
    public Result<Updated> Moderate(string status, string? note)
    {
        if (status is not (ReviewModeration.Approved or ReviewModeration.Rejected))
        {
            return ReviewErrors.Field("status", "Choose approved or rejected.");
        }

        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (status == ReviewModeration.Rejected && trimmed is null)
        {
            return ReviewErrors.Field("note", "Say why the review is not published.");
        }

        ModerationStatus = status;
        ModerationNote = status == ReviewModeration.Approved ? null : trimmed;
        return Result.Updated;
    }
}

public static class ReviewErrors
{
    public static readonly Error NotFound = Error.NotFound("Review.NotFound", "That review does not exist.");
    public static readonly Error NotReviewable = Error.Forbidden("Review.NotReviewable", "You can review only what you have bought, hired, attended or taken.");
    public static readonly Error AlreadyReviewed = Error.Conflict("Review.AlreadyReviewed", "You have already reviewed this.");

    public static Error Field(string field, string message)
        => Error.Validation($"Review.{field}", message).ForField(field);
}
