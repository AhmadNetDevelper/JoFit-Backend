using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Reviews;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Community;
using JoFit.Domain.Customers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Community;

public sealed record TopicDto(long Id, string Slug, string Name);

public sealed record AdminTopicDto(long Id, string Slug, LocalizedText Name);

public sealed record CommunityPostDto(
    long Id, string Slug, string Title, string Excerpt, string? Body, TopicDto? Topic, string AuthorName, DateTimeOffset CreatedAt, int LikeCount, int CommentCount);

public sealed record CommunityCommentDto(long Id, long PostId, long? ParentCommentId, string AuthorName, string Body, DateTimeOffset CreatedAt);

public sealed record AdminCommunityPostDto(
    long Id, string Slug, string Title, string Excerpt, string Body, long TopicId, long AuthorCustomerId, string AuthorName, string Status,
    string? ModerationNote, int ReportCount, int LikeCount, int CommentCount, DateTimeOffset CreatedAt);

public sealed record AdminCommunityCommentDto(
    long Id, long PostId, long? ParentCommentId, long AuthorCustomerId, string AuthorName, string Body, string Status, string? ModerationNote,
    int ReportCount, DateTimeOffset CreatedAt);

public sealed record GetCommunityTopicsQuery(string Language) : IRequest<Result<List<TopicDto>>>;

public sealed record GetCommunityPostsQuery(string Language, int Page, int PageSize, string? Search, IReadOnlyList<string>? TopicSlugs)
    : IRequest<Result<PagedResult<CommunityPostDto>>>;

public sealed record GetCommunityPostBySlugQuery(string Slug, string Language) : IRequest<Result<CommunityPostDto>>;

public sealed record GetCommunityCommentsQuery(long PostId) : IRequest<Result<List<CommunityCommentDto>>>;

public sealed record GetAdminCommunityPostsQuery(
    int Page, int PageSize, string? Search, IReadOnlyList<string>? Statuses, IReadOnlyList<long>? TopicIds, string? SortField, string? SortDir)
    : IRequest<Result<PagedResult<AdminCommunityPostDto>>>;

public sealed record GetAdminCommunityPostQuery(long Id) : IRequest<Result<AdminCommunityPostDto>>;

public sealed record GetAdminCommunityCommentsQuery(long PostId, int Page, int PageSize) : IRequest<Result<PagedResult<AdminCommunityCommentDto>>>;

public sealed record GetAdminCommunityTopicsQuery : IRequest<Result<List<AdminTopicDto>>>;

public sealed record CountFlaggedCommunityQuery : IRequest<Result<CountDto>>;

public sealed record ModeratePostCommand(long Id, string Status, string? Note) : IRequest<Result<AdminCommunityPostDto>>;

public sealed record ModerateCommentCommand(long Id, string Status, string? Note) : IRequest<Result<AdminCommunityCommentDto>>;

public sealed record DeletePostCommand(long Id) : IRequest<Result<Deleted>>;

public sealed record DeleteCommentCommand(long Id) : IRequest<Result<Deleted>>;

internal static class CommunitySupport
{
    public static async Task<Dictionary<long, string>> NamesAsync(IUnitOfWork unitOfWork, IEnumerable<long> customerIds, CancellationToken ct)
    {
        var ids = customerIds.Distinct().ToList();
        return await unitOfWork.GetDbContext().Set<Customer>().AsNoTracking().Where(c => ids.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name, ct);
    }

    /// <summary>"Sara K." — never a full name in public.</summary>
    public static string Short(string? name)
    {
        var parts = (name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch { 0 => "Member", 1 => parts[0], _ => $"{parts[0]} {parts[^1][0]}." };
    }

    public static AdminCommunityPostDto Admin(CommunityPost p, string? author)
        => new(p.Id, p.Slug, p.Title, p.Excerpt, p.Body, p.TopicId, p.AuthorCustomerId, author ?? string.Empty, p.Status, p.ModerationNote, p.ReportCount, p.LikeCount, p.CommentCount, p.CreatedAtUtc);

    public static AdminCommunityCommentDto Admin(CommunityComment c, string? author)
        => new(c.Id, c.PostId, c.ParentCommentId, c.AuthorCustomerId, author ?? string.Empty, c.Body, c.Status, c.ModerationNote, c.ReportCount, c.CreatedAtUtc);
}

public class GetCommunityTopicsQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization) : IRequestHandler<GetCommunityTopicsQuery, Result<List<TopicDto>>>
{
    public async Task<Result<List<TopicDto>>> Handle(GetCommunityTopicsQuery request, CancellationToken ct)
        => (await unitOfWork.GetRepository<CommunityTopic>().GetTableNoTracking().OrderBy(t => t.Id).ToListAsync(ct))
            .Select(t => new TopicDto(t.Id, t.Slug, localization.GetLocalizedName(t.NameAr, t.NameEn, request.Language))).ToList();
}

public class GetCommunityPostsQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetCommunityPostsQuery, Result<PagedResult<CommunityPostDto>>>
{
    public async Task<Result<PagedResult<CommunityPostDto>>> Handle(GetCommunityPostsQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();

        // Hidden posts are not public; flagged ones stay up until staff decide.
        var query = db.Set<CommunityPost>().AsNoTracking().Where(p => p.Status != CommunityStatuses.Hidden);
        if (request.TopicSlugs is { Count: > 0 })
        {
            var slugs = request.TopicSlugs;
            query = query.Where(p => db.Set<CommunityTopic>().Any(t => t.Id == p.TopicId && slugs.Contains(t.Slug)));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(p => p.Title.ToLower().Contains(term) || p.Excerpt.ToLower().Contains(term));
        }

        var page = await query.OrderByDescending(p => p.CreatedAtUtc).ThenByDescending(p => p.Id)
            .ToPagedResultAsync(request.Page, request.PageSize, ct, PagingExtensions.PublicMaxPageSize);
        var items = await MapAsync(unitOfWork, localization, request.Language, page.Items, includeBody: false, ct);
        return new PagedResult<CommunityPostDto>(items, page.Page, page.PageSize, page.TotalCount);
    }

    public static async Task<List<CommunityPostDto>> MapAsync(
        IUnitOfWork unitOfWork, ILocalizationService localization, string language, IReadOnlyList<CommunityPost> posts, bool includeBody, CancellationToken ct)
    {
        var topicIds = posts.Select(p => p.TopicId).Distinct().ToList();
        var topics = await unitOfWork.GetDbContext().Set<CommunityTopic>().AsNoTracking().Where(t => topicIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, ct);
        var names = await CommunitySupport.NamesAsync(unitOfWork, posts.Select(p => p.AuthorCustomerId), ct);
        return posts.Select(p =>
        {
            var topic = topics.GetValueOrDefault(p.TopicId);
            return new CommunityPostDto(
                p.Id, p.Slug, p.Title, p.Excerpt, includeBody ? p.Body : null,
                topic is null ? null : new TopicDto(topic.Id, topic.Slug, localization.GetLocalizedName(topic.NameAr, topic.NameEn, language)),
                CommunitySupport.Short(names.GetValueOrDefault(p.AuthorCustomerId)), p.CreatedAtUtc, p.LikeCount, p.CommentCount);
        }).ToList();
    }
}

public class GetCommunityPostBySlugQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetCommunityPostBySlugQuery, Result<CommunityPostDto>>
{
    public async Task<Result<CommunityPostDto>> Handle(GetCommunityPostBySlugQuery request, CancellationToken ct)
    {
        var post = await unitOfWork.GetRepository<CommunityPost>().GetTableNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == request.Slug && p.Status != CommunityStatuses.Hidden, ct);
        return post is null
            ? CommunityErrors.PostNotFound
            : (await GetCommunityPostsQueryHandler.MapAsync(unitOfWork, localization, request.Language, [post], includeBody: true, ct))[0];
    }
}

public class GetCommunityCommentsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetCommunityCommentsQuery, Result<List<CommunityCommentDto>>>
{
    public async Task<Result<List<CommunityCommentDto>>> Handle(GetCommunityCommentsQuery request, CancellationToken ct)
    {
        var rows = await unitOfWork.GetRepository<CommunityComment>().GetTableNoTracking()
            .Where(c => c.PostId == request.PostId && c.Status != CommunityStatuses.Hidden).OrderBy(c => c.CreatedAtUtc).ThenBy(c => c.Id).ToListAsync(ct);
        var names = await CommunitySupport.NamesAsync(unitOfWork, rows.Select(c => c.AuthorCustomerId), ct);
        return rows.Select(c => new CommunityCommentDto(c.Id, c.PostId, c.ParentCommentId, CommunitySupport.Short(names.GetValueOrDefault(c.AuthorCustomerId)), c.Body, c.CreatedAtUtc)).ToList();
    }
}

public class GetAdminCommunityPostsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdminCommunityPostsQuery, Result<PagedResult<AdminCommunityPostDto>>>
{
    public async Task<Result<PagedResult<AdminCommunityPostDto>>> Handle(GetAdminCommunityPostsQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<CommunityPost>().GetTableNoTracking();
        if (request.Statuses is { Count: > 0 })
        {
            var statuses = request.Statuses;
            query = query.Where(p => statuses.Contains(p.Status));
        }

        if (request.TopicIds is { Count: > 0 })
        {
            var topics = request.TopicIds;
            query = query.Where(p => topics.Contains(p.TopicId));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(p => p.Title.ToLower().Contains(term) || p.Excerpt.ToLower().Contains(term));
        }

        var ascending = string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        query = request.SortField?.ToLowerInvariant() switch
        {
            "reportcount" => ascending ? query.OrderBy(p => p.ReportCount) : query.OrderByDescending(p => p.ReportCount),
            "likecount" => ascending ? query.OrderBy(p => p.LikeCount) : query.OrderByDescending(p => p.LikeCount),
            _ => ascending ? query.OrderBy(p => p.CreatedAtUtc) : query.OrderByDescending(p => p.CreatedAtUtc)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
        var names = await CommunitySupport.NamesAsync(unitOfWork, page.Items.Select(p => p.AuthorCustomerId), ct);
        return new PagedResult<AdminCommunityPostDto>(page.Items.Select(p => CommunitySupport.Admin(p, names.GetValueOrDefault(p.AuthorCustomerId))).ToList(), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetAdminCommunityPostQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdminCommunityPostQuery, Result<AdminCommunityPostDto>>
{
    public async Task<Result<AdminCommunityPostDto>> Handle(GetAdminCommunityPostQuery request, CancellationToken ct)
    {
        var post = await unitOfWork.GetRepository<CommunityPost>().GetTableNoTracking().FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (post is null)
        {
            return CommunityErrors.PostNotFound;
        }

        var names = await CommunitySupport.NamesAsync(unitOfWork, [post.AuthorCustomerId], ct);
        return CommunitySupport.Admin(post, names.GetValueOrDefault(post.AuthorCustomerId));
    }
}

public class GetAdminCommunityCommentsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdminCommunityCommentsQuery, Result<PagedResult<AdminCommunityCommentDto>>>
{
    public async Task<Result<PagedResult<AdminCommunityCommentDto>>> Handle(GetAdminCommunityCommentsQuery request, CancellationToken ct)
    {
        // Comments of an unknown post answer an empty page, not not-found.
        var page = await unitOfWork.GetRepository<CommunityComment>().GetTableNoTracking().Where(c => c.PostId == request.PostId)
            .OrderBy(c => c.CreatedAtUtc).ThenBy(c => c.Id).ToPagedResultAsync(request.Page, request.PageSize, ct);
        var names = await CommunitySupport.NamesAsync(unitOfWork, page.Items.Select(c => c.AuthorCustomerId), ct);
        return new PagedResult<AdminCommunityCommentDto>(page.Items.Select(c => CommunitySupport.Admin(c, names.GetValueOrDefault(c.AuthorCustomerId))).ToList(), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetAdminCommunityTopicsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdminCommunityTopicsQuery, Result<List<AdminTopicDto>>>
{
    public async Task<Result<List<AdminTopicDto>>> Handle(GetAdminCommunityTopicsQuery request, CancellationToken ct)
        => (await unitOfWork.GetRepository<CommunityTopic>().GetTableNoTracking().OrderBy(t => t.Id).ToListAsync(ct))
            .Select(t => new AdminTopicDto(t.Id, t.Slug, new LocalizedText(t.NameEn, t.NameAr))).ToList();
}

public class CountFlaggedCommunityQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<CountFlaggedCommunityQuery, Result<CountDto>>
{
    public async Task<Result<CountDto>> Handle(CountFlaggedCommunityQuery request, CancellationToken ct)
    {
        // Flagged posts plus flagged comments.
        var posts = await unitOfWork.GetRepository<CommunityPost>().GetTableNoTracking().CountAsync(p => p.Status == CommunityStatuses.Flagged, ct);
        var comments = await unitOfWork.GetRepository<CommunityComment>().GetTableNoTracking().CountAsync(c => c.Status == CommunityStatuses.Flagged, ct);
        return new CountDto(posts + comments);
    }
}

public class ModeratePostCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ModeratePostCommand, Result<AdminCommunityPostDto>>
{
    public async Task<Result<AdminCommunityPostDto>> Handle(ModeratePostCommand request, CancellationToken ct)
    {
        var post = await unitOfWork.GetRepository<CommunityPost>().GetTableAsTracking().FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (post is null)
        {
            return CommunityErrors.PostNotFound;
        }

        var moderated = post.Moderate(request.Status, request.Note);
        if (moderated.IsError)
        {
            return moderated.Errors;
        }

        await unitOfWork.SaveChangesAsync(ct);
        var names = await CommunitySupport.NamesAsync(unitOfWork, [post.AuthorCustomerId], ct);
        return CommunitySupport.Admin(post, names.GetValueOrDefault(post.AuthorCustomerId));
    }
}

public class ModerateCommentCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ModerateCommentCommand, Result<AdminCommunityCommentDto>>
{
    public async Task<Result<AdminCommunityCommentDto>> Handle(ModerateCommentCommand request, CancellationToken ct)
    {
        var comment = await unitOfWork.GetRepository<CommunityComment>().GetTableAsTracking().FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (comment is null)
        {
            return CommunityErrors.CommentNotFound;
        }

        var moderated = comment.Moderate(request.Status, request.Note);
        if (moderated.IsError)
        {
            return moderated.Errors;
        }

        await unitOfWork.SaveChangesAsync(ct);
        var names = await CommunitySupport.NamesAsync(unitOfWork, [comment.AuthorCustomerId], ct);
        return CommunitySupport.Admin(comment, names.GetValueOrDefault(comment.AuthorCustomerId));
    }
}

public class DeletePostCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeletePostCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeletePostCommand request, CancellationToken ct)
    {
        var posts = unitOfWork.GetRepository<CommunityPost>();
        var post = await posts.GetTableAsTracking().FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (post is null)
        {
            return CommunityErrors.PostNotFound;
        }

        // Deleting a post deletes its comments.
        var comments = await unitOfWork.GetRepository<CommunityComment>().GetTableAsTracking().Where(c => c.PostId == post.Id).ToListAsync(ct);
        foreach (var comment in comments) unitOfWork.GetRepository<CommunityComment>().DeleteAsync(comment);
        posts.DeleteAsync(post);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}

public class DeleteCommentCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteCommentCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteCommentCommand request, CancellationToken ct)
    {
        var comments = unitOfWork.GetRepository<CommunityComment>();
        var comment = await comments.GetTableAsTracking().FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (comment is null)
        {
            return CommunityErrors.CommentNotFound;
        }

        // Deleting a comment deletes its replies (all the way down) and lowers the post's comment count.
        var all = await comments.GetTableAsTracking().Where(c => c.PostId == comment.PostId).ToListAsync(ct);
        var doomed = new List<CommunityComment> { comment };
        for (var i = 0; i < doomed.Count; i++)
        {
            doomed.AddRange(all.Where(c => c.ParentCommentId == doomed[i].Id && !doomed.Contains(c)));
        }

        foreach (var c in doomed) comments.DeleteAsync(c);
        var post = await unitOfWork.GetRepository<CommunityPost>().GetTableAsTracking().FirstOrDefaultAsync(p => p.Id == comment.PostId, ct);
        post?.LowerCommentCount(doomed.Count);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}
