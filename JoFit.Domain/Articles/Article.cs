using System.Text.RegularExpressions;
using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Articles;

public static class ArticleStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";
    public const string Archived = "archived";

    public static readonly string[] All = [Draft, Published, Archived];
}

/// <summary>The editable fields of an article, as the back-office form saves them.</summary>
public sealed record ArticleDetails(
    string Slug,
    string TitleEn,
    string TitleAr,
    string ExcerptEn,
    string ExcerptAr,
    string? BodyEn,
    string? BodyAr,
    long AuthorId,
    IReadOnlyList<string> Tags,
    IReadOnlyList<long> RelatedArticleIds);

public class Article : AuditableEntity
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    public string Slug { get; private set; } = string.Empty;
    public string TitleEn { get; private set; } = string.Empty;
    public string TitleAr { get; private set; } = string.Empty;
    public string ExcerptEn { get; private set; } = string.Empty;
    public string ExcerptAr { get; private set; } = string.Empty;
    public string? BodyEn { get; private set; }
    public string? BodyAr { get; private set; }
    public string Status { get; private set; } = ArticleStatuses.Draft;
    public long AuthorId { get; private set; }

    /// <summary>Comma-separated tag slugs.</summary>
    public string Tags { get; private set; } = string.Empty;

    /// <summary>Comma-separated ids of hand-picked related articles, shown before articles that share a sport.</summary>
    public string RelatedIds { get; private set; } = string.Empty;

    /// <summary>The largest word count across the languages, divided by 200, rounded, at least 1.</summary>
    public int ReadingMinutes { get; private set; } = 1;
    public DateTimeOffset? PublishedAtUtc { get; private set; }

    private Article()
    {
    }

    public bool IsPublished => Status == ArticleStatuses.Published;

    public IReadOnlyList<string> TagList => Tags.Split(',', StringSplitOptions.RemoveEmptyEntries);

    public IReadOnlyList<long> RelatedIdList => RelatedIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(long.Parse).ToList();

    /// <summary>A new article is always a draft.</summary>
    public static Result<Article> CreateDraft(ArticleDetails details)
    {
        var article = new Article();
        var applied = article.Apply(details);
        return applied.IsError ? applied.Errors : article;
    }

    /// <summary>
    /// A draft needs the English title (at least 3 characters) and excerpt. A published article must stay complete
    /// in every language, so a language cannot be blanked on a live article.
    /// </summary>
    public Result<Updated> Apply(ArticleDetails d)
    {
        var errors = new List<Error>();
        var slug = (d.Slug ?? string.Empty).Trim();
        var titleEn = (d.TitleEn ?? string.Empty).Trim();
        var titleAr = (d.TitleAr ?? string.Empty).Trim();
        var excerptEn = (d.ExcerptEn ?? string.Empty).Trim();
        var excerptAr = (d.ExcerptAr ?? string.Empty).Trim();
        var bodyEn = string.IsNullOrWhiteSpace(d.BodyEn) ? null : d.BodyEn.Trim();
        var bodyAr = string.IsNullOrWhiteSpace(d.BodyAr) ? null : d.BodyAr.Trim();

        if (slug.Length == 0) errors.Add(ArticleErrors.Field("slug", "A slug is required."));
        else if (!SlugPattern.IsMatch(slug)) errors.Add(ArticleErrors.Field("slug", "Use lowercase letters, numbers and single hyphens."));
        if (titleEn.Length < 3) errors.Add(ArticleErrors.Field("title.en", "The English title needs at least 3 characters."));
        if (titleEn.Length > 200 || titleAr.Length > 200) errors.Add(ArticleErrors.Field("title.en", "A title cannot exceed 200 characters."));
        if (excerptEn.Length == 0) errors.Add(ArticleErrors.Field("excerpt.en", "The English excerpt is required."));
        if (excerptEn.Length > 500 || excerptAr.Length > 500) errors.Add(ArticleErrors.Field("excerpt.en", "An excerpt cannot exceed 500 characters."));
        if (d.AuthorId <= 0) errors.Add(ArticleErrors.Field("authorId", "Choose an author."));

        if (IsPublished)
        {
            if (titleAr.Length < 3) errors.Add(ArticleErrors.Field("title.ar", "A published article needs its Arabic title."));
            if (excerptAr.Length == 0) errors.Add(ArticleErrors.Field("excerpt.ar", "A published article needs its Arabic excerpt."));
            if (bodyEn is null) errors.Add(ArticleErrors.Field("body.en", "A published article needs its English body."));
            if (bodyAr is null) errors.Add(ArticleErrors.Field("body.ar", "A published article needs its Arabic body."));
        }

        var tags = (d.Tags ?? []).Select(t => t.Trim().ToLowerInvariant()).Where(t => t.Length > 0).Distinct().ToList();
        if (tags.Count > 10 || tags.Any(t => !SlugPattern.IsMatch(t))) errors.Add(ArticleErrors.Field("tags", "Use up to 10 tags of lowercase letters, numbers and hyphens."));

        if (errors.Count > 0)
        {
            return errors;
        }

        Slug = slug;
        TitleEn = titleEn;
        TitleAr = titleAr;
        ExcerptEn = excerptEn;
        ExcerptAr = excerptAr;
        BodyEn = bodyEn;
        BodyAr = bodyAr;
        AuthorId = d.AuthorId;
        Tags = string.Join(',', tags);
        RelatedIds = string.Join(',', (d.RelatedArticleIds ?? []).Where(id => id != Id || Id == 0).Distinct());
        ReadingMinutes = ReadingTime(bodyEn, bodyAr);
        return Result.Updated;
    }

    /// <summary>Publishing checks the body in both languages (and the translated title and excerpt).</summary>
    public Result<Updated> SetStatus(string status)
    {
        if (!ArticleStatuses.All.Contains(status))
        {
            return ArticleErrors.Field("status", "Choose draft, published or archived.");
        }

        if (status == ArticleStatuses.Published)
        {
            var errors = new List<Error>();
            if (string.IsNullOrWhiteSpace(BodyEn)) errors.Add(ArticleErrors.Field("body.en", "Write the English body before publishing."));
            if (string.IsNullOrWhiteSpace(BodyAr)) errors.Add(ArticleErrors.Field("body.ar", "Write the Arabic body before publishing."));
            if (TitleAr.Length < 3) errors.Add(ArticleErrors.Field("title.ar", "Add the Arabic title before publishing."));
            if (ExcerptAr.Length == 0) errors.Add(ArticleErrors.Field("excerpt.ar", "Add the Arabic excerpt before publishing."));
            if (errors.Count > 0)
            {
                return errors;
            }

            PublishedAtUtc ??= DateTimeOffset.UtcNow;
        }

        Status = status;
        return Result.Updated;
    }

    public static int ReadingTime(string? bodyEn, string? bodyAr)
    {
        static int Words(string? text) => string.IsNullOrWhiteSpace(text) ? 0 : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Round(Math.Max(Words(bodyEn), Words(bodyAr)) / 200.0, MidpointRounding.AwayFromZero));
    }
}

public class ArticleCategoryLink
{
    public long ArticleId { get; set; }
    public long ArticleCategoryId { get; set; }
}

public class ArticleSportLink
{
    public long ArticleId { get; set; }
    public long SportId { get; set; }
}

public static class ArticleErrors
{
    public static readonly Error NotFound = Error.NotFound("Article.NotFound", "That article does not exist.");

    public static Error Field(string field, string message)
        => Error.Validation($"Article.{field}", message).ForField(field);

    public static readonly Error SlugTaken = Field("slug", "Another article already uses this slug.");
    public static readonly Error TitleTaken = Field("title.en", "Another article already has this title.");
    public static readonly Error AuthorMissing = Field("authorId", "That author does not exist.");
    public static readonly Error CategoriesMissing = Field("categoryIds", "Choose at least one category that exists.");
    public static readonly Error SportsMissing = Field("sportIds", "One of the sports does not exist.");
}
