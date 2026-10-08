using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Services;
using JoFit.Application.Features.Courses.Catalogue;
using JoFit.Application.Features.Enrolments;
using JoFit.Domain.ArticleCategories;
using JoFit.Domain.Articles;
using JoFit.Domain.Authors;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Sports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Articles;

public sealed record ArticleAuthorDto(long Id, string Name, MediaImageDto? Avatar);

public sealed record AdminArticleDto(
    long Id, string Slug, LocalizedText Title, LocalizedText Excerpt, LocalizedText? Body, string Status, long AuthorId,
    IReadOnlyList<long> CategoryIds, IReadOnlyList<long> SportIds, IReadOnlyList<string> Tags, IReadOnlyList<long> RelatedArticleIds,
    int ReadingMinutes, MediaImageDto? CoverImage, DateTimeOffset? PublishedAt, AuditDto Audit);

public sealed record PublicArticleDto(
    long Id, string Slug, string Title, string Excerpt, string? Body, ArticleAuthorDto? Author, IReadOnlyList<PublicRef> Categories,
    IReadOnlyList<string> Tags, int ReadingMinutes, MediaImageDto? CoverImage, DateTimeOffset? PublishedAt);

public sealed record PublicRef(long Id, string Slug, string Name);

public sealed record ArticleTagDto(string Slug, string Name, int Count);

/// <summary>A cover: null keeps it, an empty list clears it, one item sets it.</summary>
public sealed record ArticleInput(ArticleDetails Details, IReadOnlyList<long> CategoryIds, IReadOnlyList<long> SportIds, IReadOnlyList<GalleryItem>? Cover);

public sealed record CreateArticleCommand(ArticleInput Input) : IRequest<Result<AdminArticleDto>>;

public sealed record UpdateArticleCommand(long Id, ArticleInput Input) : IRequest<Result<AdminArticleDto>>;

public sealed record SetArticleStatusCommand(long Id, string Status) : IRequest<Result<AdminArticleDto>>;

public sealed record DeleteArticleCommand(long Id) : IRequest<Result<Deleted>>;

public sealed record GetAdminArticlesQuery(
    int Page, int PageSize, string? Search, IReadOnlyList<string>? Statuses, IReadOnlyList<long>? CategoryIds, IReadOnlyList<long>? AuthorIds,
    string? SortField, string? SortDir) : IRequest<Result<PagedResult<AdminArticleDto>>>;

public sealed record GetAdminArticleQuery(long Id) : IRequest<Result<AdminArticleDto>>;

public sealed record GetPublicArticlesQuery(
    string Language, int Page, int PageSize, string? Search, IReadOnlyList<string>? CategorySlugs, IReadOnlyList<string>? TagSlugs,
    IReadOnlyList<string>? SportSlugs, long? AuthorId) : IRequest<Result<PagedResult<PublicArticleDto>>>;

public sealed record GetPublicArticleBySlugQuery(string Slug, string Language) : IRequest<Result<PublicArticleDto>>;

public sealed record GetRelatedArticlesQuery(long ArticleId, string Language, int Limit) : IRequest<Result<List<PublicArticleDto>>>;

public sealed record GetArticleTagsQuery(string Language) : IRequest<Result<List<ArticleTagDto>>>;

internal static class ArticleSaving
{
    public const string OwnerType = "article";

    public static async Task<List<Error>> CheckReferencesAsync(IUnitOfWork unitOfWork, ArticleInput input, long? selfId, CancellationToken ct)
    {
        var errors = new List<Error>();
        var db = unitOfWork.GetDbContext();

        if (input.Details.AuthorId > 0 && !await db.Set<Author>().AnyAsync(a => a.Id == input.Details.AuthorId, ct))
            errors.Add(ArticleErrors.AuthorMissing);

        var categoryIds = input.CategoryIds.Distinct().ToList();
        if (categoryIds.Count == 0 || await db.Set<ArticleCategory>().CountAsync(c => categoryIds.Contains(c.Id), ct) != categoryIds.Count)
            errors.Add(ArticleErrors.CategoriesMissing);

        var sportIds = input.SportIds.Distinct().ToList();
        if (sportIds.Count > 0 && await db.Set<Sport>().CountAsync(s => sportIds.Contains(s.Id), ct) != sportIds.Count)
            errors.Add(ArticleErrors.SportsMissing);

        var slug = input.Details.Slug?.Trim();
        if (!string.IsNullOrEmpty(slug) && await db.Set<Article>().AnyAsync(a => a.Slug == slug && a.Id != selfId, ct))
            errors.Add(ArticleErrors.SlugTaken);

        var title = input.Details.TitleEn?.Trim().ToLower();
        if (!string.IsNullOrEmpty(title) && await db.Set<Article>().AnyAsync(a => a.TitleEn.ToLower() == title && a.Id != selfId, ct))
            errors.Add(ArticleErrors.TitleTaken);

        return errors;
    }

    public static async Task ReplaceLinksAsync(IUnitOfWork unitOfWork, long articleId, ArticleInput input, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        db.RemoveRange(await db.Set<ArticleCategoryLink>().Where(l => l.ArticleId == articleId).ToListAsync(ct));
        db.RemoveRange(await db.Set<ArticleSportLink>().Where(l => l.ArticleId == articleId).ToListAsync(ct));
        db.AddRange(input.CategoryIds.Distinct().Select(id => new ArticleCategoryLink { ArticleId = articleId, ArticleCategoryId = id }));
        db.AddRange(input.SportIds.Distinct().Select(id => new ArticleSportLink { ArticleId = articleId, SportId = id }));
    }

    public static async Task<AdminArticleDto> ToDtoAsync(IUnitOfWork unitOfWork, long id, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var article = await db.Set<Article>().AsNoTracking().FirstAsync(a => a.Id == id, ct);
        var categoryIds = await db.Set<ArticleCategoryLink>().AsNoTracking().Where(l => l.ArticleId == id).Select(l => l.ArticleCategoryId).OrderBy(x => x).ToListAsync(ct);
        var sportIds = await db.Set<ArticleSportLink>().AsNoTracking().Where(l => l.ArticleId == id).Select(l => l.SportId).OrderBy(x => x).ToListAsync(ct);
        var cover = await GalleryService.LoadAsync(unitOfWork, OwnerType, "cover", [id], ct);
        return Map(article, categoryIds, sportIds, cover.GetValueOrDefault(id)?.FirstOrDefault());
    }

    public static AdminArticleDto Map(Article a, IReadOnlyList<long> categoryIds, IReadOnlyList<long> sportIds, MediaImageDto? cover)
        => new(
            a.Id, a.Slug, new LocalizedText(a.TitleEn, a.TitleAr), new LocalizedText(a.ExcerptEn, a.ExcerptAr),
            a.BodyEn is null || a.BodyAr is null ? null : new LocalizedText(a.BodyEn, a.BodyAr),
            a.Status, a.AuthorId, categoryIds, sportIds, a.TagList, a.RelatedIdList, a.ReadingMinutes, cover, a.PublishedAtUtc, AuditDto.From(a));
}

public class CreateArticleCommandHandler(IUnitOfWork unitOfWork, IUrlService urlService) : IRequestHandler<CreateArticleCommand, Result<AdminArticleDto>>
{
    public async Task<Result<AdminArticleDto>> Handle(CreateArticleCommand request, CancellationToken ct)
    {
        var input = request.Input;
        var errors = await ArticleSaving.CheckReferencesAsync(unitOfWork, input, null, ct);
        var created = Article.CreateDraft(input.Details);
        if (created.IsError) errors.AddRange(created.Errors);
        if (errors.Count > 0)
        {
            return errors;
        }

        var article = created.Value;
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await unitOfWork.GetRepository<Article>().AddAsync(article, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await ArticleSaving.ReplaceLinksAsync(unitOfWork, article.Id, input, ct);
            if (input.Cover is not null)
            {
                var cover = await GalleryService.SyncAsync(unitOfWork, urlService, ArticleSaving.OwnerType, article.Id, "cover", input.Cover.Take(1).ToList(), ct);
                if (cover.IsError)
                {
                    await unitOfWork.RollbackAsync(ct);
                    return cover.Errors;
                }
            }

            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }

        return await ArticleSaving.ToDtoAsync(unitOfWork, article.Id, ct);
    }
}

public class UpdateArticleCommandHandler(IUnitOfWork unitOfWork, IUrlService urlService) : IRequestHandler<UpdateArticleCommand, Result<AdminArticleDto>>
{
    public async Task<Result<AdminArticleDto>> Handle(UpdateArticleCommand request, CancellationToken ct)
    {
        var article = await unitOfWork.GetRepository<Article>().GetTableAsTracking().FirstOrDefaultAsync(a => a.Id == request.Id, ct);
        if (article is null)
        {
            return ArticleErrors.NotFound;
        }

        var errors = await ArticleSaving.CheckReferencesAsync(unitOfWork, request.Input, article.Id, ct);
        var applied = article.Apply(request.Input.Details with { RelatedArticleIds = request.Input.Details.RelatedArticleIds.Where(id => id != article.Id).ToList() });
        if (applied.IsError) errors.AddRange(applied.Errors);
        if (errors.Count > 0)
        {
            return errors;
        }

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await ArticleSaving.ReplaceLinksAsync(unitOfWork, article.Id, request.Input, ct);
            if (request.Input.Cover is not null)
            {
                var cover = await GalleryService.SyncAsync(unitOfWork, urlService, ArticleSaving.OwnerType, article.Id, "cover", request.Input.Cover.Take(1).ToList(), ct);
                if (cover.IsError)
                {
                    await unitOfWork.RollbackAsync(ct);
                    return cover.Errors;
                }
            }

            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }

        return await ArticleSaving.ToDtoAsync(unitOfWork, article.Id, ct);
    }
}

public class SetArticleStatusCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<SetArticleStatusCommand, Result<AdminArticleDto>>
{
    public async Task<Result<AdminArticleDto>> Handle(SetArticleStatusCommand request, CancellationToken ct)
    {
        var article = await unitOfWork.GetRepository<Article>().GetTableAsTracking().FirstOrDefaultAsync(a => a.Id == request.Id, ct);
        if (article is null)
        {
            return ArticleErrors.NotFound;
        }

        var result = article.SetStatus(request.Status);
        if (result.IsError)
        {
            return result.Errors;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return await ArticleSaving.ToDtoAsync(unitOfWork, article.Id, ct);
    }
}

public class DeleteArticleCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteArticleCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteArticleCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<Article>();
        var article = await repository.GetTableAsTracking().FirstOrDefaultAsync(a => a.Id == request.Id, ct);
        if (article is null)
        {
            return ArticleErrors.NotFound;
        }

        repository.DeleteAsync(article);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}

public class GetAdminArticlesQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdminArticlesQuery, Result<PagedResult<AdminArticleDto>>>
{
    public async Task<Result<PagedResult<AdminArticleDto>>> Handle(GetAdminArticlesQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var query = db.Set<Article>().AsNoTracking().AsQueryable();

        if (request.Statuses is { Count: > 0 })
        {
            var statuses = request.Statuses;
            query = query.Where(a => statuses.Contains(a.Status));
        }

        if (request.AuthorIds is { Count: > 0 })
        {
            var authors = request.AuthorIds;
            query = query.Where(a => authors.Contains(a.AuthorId));
        }

        if (request.CategoryIds is { Count: > 0 })
        {
            var categories = request.CategoryIds;
            query = query.Where(a => db.Set<ArticleCategoryLink>().Any(l => l.ArticleId == a.Id && categories.Contains(l.ArticleCategoryId)));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(a => a.TitleEn.ToLower().Contains(term) || a.TitleAr.ToLower().Contains(term)
                                     || a.ExcerptEn.ToLower().Contains(term) || a.ExcerptAr.ToLower().Contains(term) || a.Slug.Contains(term));
        }

        var descending = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = request.SortField?.ToLowerInvariant() switch
        {
            "title" => descending ? query.OrderByDescending(a => a.TitleEn) : query.OrderBy(a => a.TitleEn),
            "status" => descending ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            "publishedat" => descending ? query.OrderByDescending(a => a.PublishedAtUtc) : query.OrderBy(a => a.PublishedAtUtc),
            "updatedat" => descending ? query.OrderByDescending(a => a.LastModifiedUtc ?? a.CreatedAtUtc) : query.OrderBy(a => a.LastModifiedUtc ?? a.CreatedAtUtc),
            _ => query.OrderBy(a => a.Id)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
        var ids = page.Items.Select(a => a.Id).ToList();
        var categoryLinks = await db.Set<ArticleCategoryLink>().AsNoTracking().Where(l => ids.Contains(l.ArticleId)).ToListAsync(ct);
        var sportLinks = await db.Set<ArticleSportLink>().AsNoTracking().Where(l => ids.Contains(l.ArticleId)).ToListAsync(ct);
        var covers = await GalleryService.LoadAsync(unitOfWork, "article", "cover", ids, ct);

        var items = page.Items.Select(a => ArticleSaving.Map(
            a,
            categoryLinks.Where(l => l.ArticleId == a.Id).Select(l => l.ArticleCategoryId).OrderBy(x => x).ToList(),
            sportLinks.Where(l => l.ArticleId == a.Id).Select(l => l.SportId).OrderBy(x => x).ToList(),
            covers.GetValueOrDefault(a.Id)?.FirstOrDefault())).ToList();
        return new PagedResult<AdminArticleDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetAdminArticleQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdminArticleQuery, Result<AdminArticleDto>>
{
    public async Task<Result<AdminArticleDto>> Handle(GetAdminArticleQuery request, CancellationToken ct)
    {
        var exists = await unitOfWork.GetRepository<Article>().GetTableNoTracking().AnyAsync(a => a.Id == request.Id, ct);
        return exists ? await ArticleSaving.ToDtoAsync(unitOfWork, request.Id, ct) : ArticleErrors.NotFound;
    }
}

public class GetPublicArticlesQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicArticlesQuery, Result<PagedResult<PublicArticleDto>>>
{
    public async Task<Result<PagedResult<PublicArticleDto>>> Handle(GetPublicArticlesQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var arabic = localization.NormalizeLanguage(request.Language) == "ar";
        var query = db.Set<Article>().AsNoTracking().Where(a => a.Status == ArticleStatuses.Published);

        if (request.AuthorId is long authorId) query = query.Where(a => a.AuthorId == authorId);

        if (request.CategorySlugs is { Count: > 0 })
        {
            var slugs = request.CategorySlugs;
            query = query.Where(a => db.Set<ArticleCategoryLink>().Any(l => l.ArticleId == a.Id
                && db.Set<ArticleCategory>().Any(c => c.Id == l.ArticleCategoryId && c.Slug != null && slugs.Contains(c.Slug))));
        }

        if (request.SportSlugs is { Count: > 0 })
        {
            var slugs = request.SportSlugs;
            query = query.Where(a => db.Set<ArticleSportLink>().Any(l => l.ArticleId == a.Id
                && db.Set<Sport>().Any(s => s.Id == l.SportId && s.Slug != null && slugs.Contains(s.Slug))));
        }

        if (request.TagSlugs is { Count: > 0 })
        {
            var tags = request.TagSlugs;
            query = query.Where(a => tags.Any(t => ("," + a.Tags + ",").Contains("," + t + ",")));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = arabic
                ? query.Where(a => a.TitleAr.ToLower().Contains(term) || a.ExcerptAr.ToLower().Contains(term))
                : query.Where(a => a.TitleEn.ToLower().Contains(term) || a.ExcerptEn.ToLower().Contains(term));
        }

        var page = await query.OrderByDescending(a => a.PublishedAtUtc).ThenByDescending(a => a.Id)
            .ToPagedResultAsync(request.Page, request.PageSize, ct, PagingExtensions.PublicMaxPageSize);
        var items = await PublicArticleMapper.MapAsync(unitOfWork, localization, request.Language, page.Items.Select(a => a.Id).ToList(), includeBody: false, ct);
        return new PagedResult<PublicArticleDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetPublicArticleBySlugQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetPublicArticleBySlugQuery, Result<PublicArticleDto>>
{
    public async Task<Result<PublicArticleDto>> Handle(GetPublicArticleBySlugQuery request, CancellationToken ct)
    {
        var id = await unitOfWork.GetRepository<Article>().GetTableNoTracking()
            .Where(a => a.Slug == request.Slug && a.Status == ArticleStatuses.Published).Select(a => (long?)a.Id).FirstOrDefaultAsync(ct);
        if (id is null)
        {
            return ArticleErrors.NotFound;
        }

        return (await PublicArticleMapper.MapAsync(unitOfWork, localization, request.Language, [id.Value], includeBody: true, ct))[0];
    }
}

public class GetRelatedArticlesQueryHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<GetRelatedArticlesQuery, Result<List<PublicArticleDto>>>
{
    public async Task<Result<List<PublicArticleDto>>> Handle(GetRelatedArticlesQuery request, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var source = await db.Set<Article>().AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.ArticleId, ct);
        if (source is null)
        {
            return new List<PublicArticleDto>();
        }

        var limit = Math.Clamp(request.Limit, 1, 20);
        var published = db.Set<Article>().AsNoTracking().Where(a => a.Status == ArticleStatuses.Published && a.Id != source.Id);

        // Hand-picked related articles first, in the order they were chosen, then articles that share a sport.
        var pickedIds = source.RelatedIdList;
        var picked = await published.Where(a => pickedIds.Contains(a.Id)).Select(a => a.Id).ToListAsync(ct);
        var ids = pickedIds.Where(picked.Contains).ToList();

        var sportIds = await db.Set<ArticleSportLink>().AsNoTracking().Where(l => l.ArticleId == source.Id).Select(l => l.SportId).ToListAsync(ct);
        if (ids.Count < limit && sportIds.Count > 0)
        {
            var shared = await published.Where(a => !ids.Contains(a.Id) && db.Set<ArticleSportLink>().Any(l => l.ArticleId == a.Id && sportIds.Contains(l.SportId)))
                .OrderByDescending(a => a.PublishedAtUtc).Select(a => a.Id).Take(limit - ids.Count).ToListAsync(ct);
            ids.AddRange(shared);
        }

        return await PublicArticleMapper.MapAsync(unitOfWork, localization, request.Language, ids.Take(limit).ToList(), includeBody: false, ct);
    }
}

public class GetArticleTagsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetArticleTagsQuery, Result<List<ArticleTagDto>>>
{
    public async Task<Result<List<ArticleTagDto>>> Handle(GetArticleTagsQuery request, CancellationToken ct)
    {
        var all = await unitOfWork.GetRepository<Article>().GetTableNoTracking()
            .Where(a => a.Status == ArticleStatuses.Published && a.Tags != string.Empty).Select(a => a.Tags).ToListAsync(ct);
        return all.SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .GroupBy(t => t).Select(g => new ArticleTagDto(g.Key, g.Key, g.Count())).OrderByDescending(t => t.Count).ThenBy(t => t.Slug).ToList();
    }
}

internal static class PublicArticleMapper
{
    public static async Task<List<PublicArticleDto>> MapAsync(
        IUnitOfWork unitOfWork, ILocalizationService localization, string language, IReadOnlyList<long> ids, bool includeBody, CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var db = unitOfWork.GetDbContext();
        var articles = await db.Set<Article>().AsNoTracking().Where(a => ids.Contains(a.Id)).ToListAsync(ct);
        var authorIds = articles.Select(a => a.AuthorId).Distinct().ToList();
        var authors = await db.Set<Author>().AsNoTracking().Where(a => authorIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, ct);
        var links = await db.Set<ArticleCategoryLink>().AsNoTracking().Where(l => ids.Contains(l.ArticleId)).ToListAsync(ct);
        var categoryIds = links.Select(l => l.ArticleCategoryId).Distinct().ToList();
        var categories = await db.Set<ArticleCategory>().AsNoTracking().Where(c => categoryIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
        var covers = await GalleryService.LoadAsync(unitOfWork, "article", "cover", ids, ct);

        return ids.Select(id => articles.First(a => a.Id == id)).Select(a =>
        {
            var author = authors.GetValueOrDefault(a.AuthorId);
            return new PublicArticleDto(
                a.Id,
                a.Slug,
                localization.GetLocalizedName(a.TitleAr, a.TitleEn, language),
                localization.GetLocalizedName(a.ExcerptAr, a.ExcerptEn, language),
                includeBody && a.BodyEn is not null && a.BodyAr is not null ? localization.GetLocalizedName(a.BodyAr, a.BodyEn, language) : null,
                author is null ? null : new ArticleAuthorDto(
                    author.Id, author.Name,
                    author.AvatarUrl is null ? null : new MediaImageDto(author.AvatarMediaId ?? string.Empty, author.AvatarUrl, author.AvatarAlt ?? string.Empty)),
                links.Where(l => l.ArticleId == a.Id).Select(l => categories.GetValueOrDefault(l.ArticleCategoryId)).Where(c => c is not null)
                    .Select(c => new PublicRef(c!.Id, c.Slug ?? string.Empty, localization.GetLocalizedName(c.NameAr, c.NameEn, language))).OrderBy(c => c.Name).ToList(),
                a.TagList,
                a.ReadingMinutes,
                covers.GetValueOrDefault(a.Id)?.FirstOrDefault(),
                a.PublishedAtUtc);
        }).ToList();
    }
}
