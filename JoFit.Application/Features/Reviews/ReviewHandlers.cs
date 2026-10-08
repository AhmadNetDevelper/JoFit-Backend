using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Services;
using JoFit.Application.Features.Customers;
using JoFit.Application.Features.Notifications;
using JoFit.Domain.Coaches;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Courses;
using JoFit.Domain.Customers;
using JoFit.Domain.Enrolments;
using JoFit.Domain.Notifications;
using JoFit.Domain.Orders;
using JoFit.Domain.Products;
using JoFit.Domain.Rental;
using JoFit.Domain.Reviews;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Reviews;

public sealed record ReviewableDto(string Type, long Id, string Title);

public sealed record AccountReviewDto(
    long Id, string TargetType, long TargetId, string TargetTitle, int Rating, string? Title, string Body, string Status, string? ModerationNote, DateTimeOffset SubmittedAt);

public sealed record PublicReviewDto(
    long Id, string TargetType, long TargetId, int Rating, string? Title, string Body, string AuthorName, DateTimeOffset CreatedAt, int HelpfulCount, bool IsVerified);

public sealed record RatingSummaryDto(double Average, int Count, IReadOnlyDictionary<string, int> Distribution);

public sealed record ReviewCollectionDto(IReadOnlyList<PublicReviewDto> Items, RatingSummaryDto Summary, PageMeta Meta);

public sealed record AdminReviewDto(
    long Id, string TargetType, long TargetId, LocalizedText TargetTitle, long CustomerId, string? CustomerName, int Rating, string? Title, string Body,
    string ModerationStatus, string? ModerationNote, int HelpfulCount, DateTimeOffset SubmittedAt);

public sealed record GetMyReviewableQuery(long UserId, string Language) : IRequest<Result<List<ReviewableDto>>>;

public sealed record GetMyReviewsQuery(long UserId) : IRequest<Result<List<AccountReviewDto>>>;

public sealed record SubmitReviewCommand(long UserId, string? Type, long Id, int Rating, string? Title, string? Body) : IRequest<Result<Deleted>>;

public sealed record GetPublicReviewsQuery(string? TargetType, long? TargetId, int Page, int PageSize) : IRequest<Result<ReviewCollectionDto>>;

public sealed record GetRatingSummaryQuery(string? TargetType, long? TargetId) : IRequest<Result<RatingSummaryDto>>;

public sealed record GetRecentReviewsQuery(int Limit) : IRequest<Result<List<PublicReviewDto>>>;

public sealed record GetAdminReviewsQuery(
    int Page, int PageSize, string? Search, IReadOnlyList<string>? ModerationStatuses, IReadOnlyList<string>? TargetTypes, int? MinRating,
    string? SortField, string? SortDir) : IRequest<Result<PagedResult<AdminReviewDto>>>;

public sealed record GetAdminReviewQuery(long Id) : IRequest<Result<AdminReviewDto>>;

public sealed record CountPendingReviewsQuery : IRequest<Result<CountDto>>;

public sealed record CountDto(int Count);

public sealed record ModerateReviewCommand(long Id, string Status, string? Note) : IRequest<Result<AdminReviewDto>>;

public sealed record DeleteReviewCommand(long Id) : IRequest<Result<Deleted>>;

internal static class ReviewSupport
{
    /// <summary>What this customer has bought, hired, attended or taken, and has not reviewed yet.</summary>
    public static async Task<List<(string Type, long Id)>> ReviewableAsync(IUnitOfWork unitOfWork, long customerId, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var result = new List<(string, long)>();

        var products = await db.Set<Order>().AsNoTracking()
            .Where(o => o.CustomerId == customerId && o.Status == OrderStatuses.Completed)
            .SelectMany(o => o.Lines).Where(l => l.ProductId != null).Select(l => l.ProductId!.Value).Distinct().ToListAsync(ct);
        result.AddRange(products.Select(id => (ReviewTargets.Product, id)));

        var rentals = await db.Set<RentalBooking>().AsNoTracking()
            .Where(b => b.CustomerId == customerId && b.Status == RentalBookingStatuses.Returned).Select(b => b.RentalItemId).Distinct().ToListAsync(ct);
        result.AddRange(rentals.Select(id => (ReviewTargets.RentalItem, id)));

        var coaches = await db.Set<CoachBooking>().AsNoTracking()
            .Where(b => b.CustomerId == customerId && b.Status == CoachBookingStatuses.Completed).Select(b => b.CoachId).Distinct().ToListAsync(ct);
        result.AddRange(coaches.Select(id => (ReviewTargets.Coach, id)));

        var courses = await db.Set<Enrolment>().AsNoTracking()
            .Where(e => e.CustomerId == customerId && (e.Status == EnrolmentStatuses.Active || e.Status == EnrolmentStatuses.Completed))
            .Select(e => e.CourseId).Distinct().ToListAsync(ct);
        result.AddRange(courses.Select(id => (ReviewTargets.Course, id)));

        var reviewed = (await db.Set<Review>().AsNoTracking().Where(r => r.CustomerId == customerId).Select(r => new { r.TargetType, r.TargetId }).ToListAsync(ct))
            .Select(r => (r.TargetType, r.TargetId)).ToHashSet();
        return result.Where(r => !reviewed.Contains(r)).ToList();
    }

    /// <summary>English and Arabic titles of review targets, keyed by (type, id).</summary>
    public static async Task<Dictionary<(string, long), LocalizedText>> TitlesAsync(IUnitOfWork unitOfWork, IEnumerable<(string Type, long Id)> targets, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var list = targets.Distinct().ToList();
        var result = new Dictionary<(string, long), LocalizedText>();

        var ids = list.Where(t => t.Type == ReviewTargets.Product).Select(t => t.Id).ToList();
        foreach (var p in await db.Set<Product>().IgnoreQueryFilters().AsNoTracking().Where(p => ids.Contains(p.Id)).Select(p => new { p.Id, p.TitleEn, p.TitleAr }).ToListAsync(ct))
            result[(ReviewTargets.Product, p.Id)] = new LocalizedText(p.TitleEn, p.TitleAr);

        ids = list.Where(t => t.Type == ReviewTargets.RentalItem).Select(t => t.Id).ToList();
        foreach (var p in await db.Set<RentalItem>().IgnoreQueryFilters().AsNoTracking().Where(p => ids.Contains(p.Id)).Select(p => new { p.Id, p.TitleEn, p.TitleAr }).ToListAsync(ct))
            result[(ReviewTargets.RentalItem, p.Id)] = new LocalizedText(p.TitleEn, p.TitleAr);

        ids = list.Where(t => t.Type == ReviewTargets.Coach).Select(t => t.Id).ToList();
        foreach (var p in await db.Set<Coach>().IgnoreQueryFilters().AsNoTracking().Where(p => ids.Contains(p.Id)).Select(p => new { p.Id, p.NameEn, p.NameAr }).ToListAsync(ct))
            result[(ReviewTargets.Coach, p.Id)] = new LocalizedText(p.NameEn, p.NameAr);

        ids = list.Where(t => t.Type == ReviewTargets.Course).Select(t => t.Id).ToList();
        foreach (var p in await db.Set<Course>().IgnoreQueryFilters().AsNoTracking().Where(p => ids.Contains(p.Id)).Select(p => new { p.Id, p.NameEn, p.NameAr }).ToListAsync(ct))
            result[(ReviewTargets.Course, p.Id)] = new LocalizedText(p.NameEn, p.NameAr);

        return result;
    }

    public static RatingSummaryDto Summary(IReadOnlyCollection<int> ratings)
    {
        var distribution = Enumerable.Range(1, 5).ToDictionary(n => n.ToString(), n => ratings.Count(r => r == n));
        return new RatingSummaryDto(ratings.Count == 0 ? 0 : Math.Round(ratings.Average(), 1), ratings.Count, distribution);
    }

    public static async Task<List<PublicReviewDto>> PublicAsync(IUnitOfWork unitOfWork, IReadOnlyList<Review> reviews, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var customerIds = reviews.Select(r => r.CustomerId).Distinct().ToList();
        var names = await db.Set<Customer>().AsNoTracking().Where(c => customerIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        // "Sara K." — the first name and the initial of the last, never the full name.
        string Author(long id)
        {
            var parts = (names.GetValueOrDefault(id) ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch { 0 => "Customer", 1 => parts[0], _ => $"{parts[0]} {parts[^1][0]}." };
        }

        return reviews.Select(r => new PublicReviewDto(r.Id, r.TargetType, r.TargetId, r.Rating, r.Title, r.Body, Author(r.CustomerId), r.SubmittedAtUtc, r.HelpfulCount, true)).ToList();
    }

    public static async Task<List<AdminReviewDto>> AdminAsync(IUnitOfWork unitOfWork, IReadOnlyList<Review> reviews, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var customerIds = reviews.Select(r => r.CustomerId).Distinct().ToList();
        var names = await db.Set<Customer>().AsNoTracking().Where(c => customerIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name, ct);
        var titles = await TitlesAsync(unitOfWork, reviews.Select(r => (r.TargetType, r.TargetId)), ct);

        return reviews.Select(r => new AdminReviewDto(
            r.Id, r.TargetType, r.TargetId, titles.GetValueOrDefault((r.TargetType, r.TargetId)) ?? new LocalizedText(string.Empty, string.Empty),
            r.CustomerId, names.GetValueOrDefault(r.CustomerId), r.Rating, r.Title, r.Body, r.ModerationStatus, r.ModerationNote, r.HelpfulCount, r.SubmittedAtUtc)).ToList();
    }
}

public class GetMyReviewableQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetMyReviewableQuery, Result<List<ReviewableDto>>>
{
    public async Task<Result<List<ReviewableDto>>> Handle(GetMyReviewableQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var customerId = await db.Set<Customer>().AsNoTracking().Where(c => c.UserId == request.UserId).Select(c => (long?)c.Id).FirstOrDefaultAsync(ct);
        if (customerId is null)
        {
            return CustomerErrors.NotFound;
        }

        var targets = await ReviewSupport.ReviewableAsync(unitOfWork, customerId.Value, ct);
        var titles = await ReviewSupport.TitlesAsync(unitOfWork, targets, ct);
        return targets.Select(t =>
        {
            var title = titles.GetValueOrDefault(t);
            return new ReviewableDto(t.Type, t.Id, title is null ? string.Empty : localization.GetLocalizedName(title.Ar, title.En, request.Language));
        }).ToList();
    }
}

public class GetMyReviewsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetMyReviewsQuery, Result<List<AccountReviewDto>>>
{
    public async Task<Result<List<AccountReviewDto>>> Handle(GetMyReviewsQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var customerId = await db.Set<Customer>().AsNoTracking().Where(c => c.UserId == request.UserId).Select(c => (long?)c.Id).FirstOrDefaultAsync(ct);
        var rows = await db.Set<Review>().AsNoTracking().Where(r => r.CustomerId == customerId).OrderByDescending(r => r.SubmittedAtUtc).ToListAsync(ct);
        var titles = await ReviewSupport.TitlesAsync(unitOfWork, rows.Select(r => (r.TargetType, r.TargetId)), ct);
        return rows.Select(r => new AccountReviewDto(
            r.Id, r.TargetType, r.TargetId, titles.GetValueOrDefault((r.TargetType, r.TargetId))?.En ?? string.Empty, r.Rating, r.Title, r.Body,
            r.ModerationStatus, r.ModerationStatus == ReviewModeration.Rejected ? r.ModerationNote : null, r.SubmittedAtUtc)).ToList();
    }
}

public class SubmitReviewCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<SubmitReviewCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(SubmitReviewCommand request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var customer = await db.Set<Customer>().AsNoTracking().FirstOrDefaultAsync(c => c.UserId == request.UserId, ct);
        if (customer is null)
        {
            return CustomerErrors.NotFound;
        }

        if (request.Type is null || !ReviewTargets.All.Contains(request.Type))
        {
            return ReviewErrors.Field("type", "Choose what you are reviewing.");
        }

        var submitted = Review.Submit(customer.Id, request.Type, request.Id, request.Rating, request.Title, request.Body);
        if (submitted.IsError)
        {
            return submitted.Errors;
        }

        // Only what the customer actually has can be reviewed; anything else is forbidden.
        var reviewable = await ReviewSupport.ReviewableAsync(unitOfWork, customer.Id, ct);
        if (!reviewable.Contains((request.Type, request.Id)))
        {
            return ReviewErrors.NotReviewable;
        }

        try
        {
            await unitOfWork.GetRepository<Review>().AddAsync(submitted.Value, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ReviewErrors.AlreadyReviewed;
        }

        NotificationWriter.ToStaff(unitOfWork, NotificationKinds.ReviewSubmitted,
            ("Review submitted", "تم إرسال تقييم"),
            ($"{customer.Name} left a {request.Rating}-star review that needs moderation.", $"{customer.Name} أضاف تقييماً من {request.Rating} نجوم يحتاج إلى مراجعة."),
            "/reviews");
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}

public class GetPublicReviewsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetPublicReviewsQuery, Result<ReviewCollectionDto>>
{
    public async Task<Result<ReviewCollectionDto>> Handle(GetPublicReviewsQuery request, CancellationToken ct)
    {
        // Only approved reviews are public, newest first.
        var all = unitOfWork.GetRepository<Review>().GetTableNoTracking().Where(r => r.ModerationStatus == ReviewModeration.Approved);
        if (request.TargetType is not null) all = all.Where(r => r.TargetType == request.TargetType);
        if (request.TargetId is long id) all = all.Where(r => r.TargetId == id);

        // The summary covers every public review of the target, not only this page.
        var summary = ReviewSupport.Summary(await all.Select(r => r.Rating).ToListAsync(ct));
        var page = await all.OrderByDescending(r => r.SubmittedAtUtc).ThenByDescending(r => r.Id)
            .ToPagedResultAsync(request.Page, request.PageSize, ct, PagingExtensions.PublicMaxPageSize);
        var items = await ReviewSupport.PublicAsync(unitOfWork, page.Items.ToList(), ct);
        return new ReviewCollectionDto(items, summary, new PagedResult<PublicReviewDto>(items, page.Page, page.PageSize, page.TotalCount).Meta);
    }
}

public class GetRatingSummaryQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetRatingSummaryQuery, Result<RatingSummaryDto>>
{
    public async Task<Result<RatingSummaryDto>> Handle(GetRatingSummaryQuery request, CancellationToken ct)
    {
        var all = unitOfWork.GetRepository<Review>().GetTableNoTracking().Where(r => r.ModerationStatus == ReviewModeration.Approved);
        if (request.TargetType is not null) all = all.Where(r => r.TargetType == request.TargetType);
        if (request.TargetId is long id) all = all.Where(r => r.TargetId == id);
        return ReviewSupport.Summary(await all.Select(r => r.Rating).ToListAsync(ct));
    }
}

public class GetRecentReviewsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetRecentReviewsQuery, Result<List<PublicReviewDto>>>
{
    public async Task<Result<List<PublicReviewDto>>> Handle(GetRecentReviewsQuery request, CancellationToken ct)
    {
        var rows = await unitOfWork.GetRepository<Review>().GetTableNoTracking()
            .Where(r => r.ModerationStatus == ReviewModeration.Approved)
            .OrderByDescending(r => r.SubmittedAtUtc).ThenByDescending(r => r.Id).Take(Math.Clamp(request.Limit, 1, 50)).ToListAsync(ct);
        return await ReviewSupport.PublicAsync(unitOfWork, rows, ct);
    }
}

public class GetAdminReviewsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdminReviewsQuery, Result<PagedResult<AdminReviewDto>>>
{
    public async Task<Result<PagedResult<AdminReviewDto>>> Handle(GetAdminReviewsQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var query = db.Set<Review>().AsNoTracking().AsQueryable();

        if (request.ModerationStatuses is { Count: > 0 })
        {
            var statuses = request.ModerationStatuses;
            query = query.Where(r => statuses.Contains(r.ModerationStatus));
        }

        if (request.TargetTypes is { Count: > 0 })
        {
            var types = request.TargetTypes;
            query = query.Where(r => types.Contains(r.TargetType));
        }

        if (request.MinRating is int min) query = query.Where(r => r.Rating >= min);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(r => r.Body.ToLower().Contains(term) || (r.Title != null && r.Title.ToLower().Contains(term))
                                     || db.Set<Customer>().Any(c => c.Id == r.CustomerId && c.Name.ToLower().Contains(term)));
        }

        var ascending = string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        query = request.SortField?.ToLowerInvariant() switch
        {
            "rating" => ascending ? query.OrderBy(r => r.Rating) : query.OrderByDescending(r => r.Rating),
            "moderationstatus" => ascending ? query.OrderBy(r => r.ModerationStatus) : query.OrderByDescending(r => r.ModerationStatus),
            _ => ascending ? query.OrderBy(r => r.SubmittedAtUtc) : query.OrderByDescending(r => r.SubmittedAtUtc)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
        return new PagedResult<AdminReviewDto>(await ReviewSupport.AdminAsync(unitOfWork, page.Items.ToList(), ct), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetAdminReviewQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdminReviewQuery, Result<AdminReviewDto>>
{
    public async Task<Result<AdminReviewDto>> Handle(GetAdminReviewQuery request, CancellationToken ct)
    {
        var review = await unitOfWork.GetRepository<Review>().GetTableNoTracking().FirstOrDefaultAsync(r => r.Id == request.Id, ct);
        return review is null ? ReviewErrors.NotFound : (await ReviewSupport.AdminAsync(unitOfWork, [review], ct))[0];
    }
}

public class CountPendingReviewsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<CountPendingReviewsQuery, Result<CountDto>>
{
    public async Task<Result<CountDto>> Handle(CountPendingReviewsQuery request, CancellationToken ct)
        => new CountDto(await unitOfWork.GetRepository<Review>().GetTableNoTracking().CountAsync(r => r.ModerationStatus == ReviewModeration.Pending, ct));
}

public class ModerateReviewCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ModerateReviewCommand, Result<AdminReviewDto>>
{
    public async Task<Result<AdminReviewDto>> Handle(ModerateReviewCommand request, CancellationToken ct)
    {
        var review = await unitOfWork.GetRepository<Review>().GetTableAsTracking().FirstOrDefaultAsync(r => r.Id == request.Id, ct);
        if (review is null)
        {
            return ReviewErrors.NotFound;
        }

        var moderated = review.Moderate(request.Status, request.Note);
        if (moderated.IsError)
        {
            return moderated.Errors;
        }

        var published = request.Status == ReviewModeration.Approved;
        NotificationWriter.ToCustomer(unitOfWork, review.CustomerId, NotificationKinds.ReviewModerated,
            published ? ("Your review is published", "تم نشر تقييمك") : ("Your review was not published", "لم يتم نشر تقييمك"),
            published ? ("Thank you — your review is now live.", "شكراً لك، أصبح تقييمك ظاهراً للجميع.") : ($"We could not publish your review: {review.ModerationNote}", $"تعذّر نشر تقييمك: {review.ModerationNote}"),
            "/account/reviews");
        await unitOfWork.SaveChangesAsync(ct);
        return (await ReviewSupport.AdminAsync(unitOfWork, [review], ct))[0];
    }
}

public class DeleteReviewCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteReviewCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteReviewCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<Review>();
        var review = await repository.GetTableAsTracking().FirstOrDefaultAsync(r => r.Id == request.Id, ct);
        if (review is null)
        {
            return ReviewErrors.NotFound;
        }

        repository.DeleteAsync(review);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}
