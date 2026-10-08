using System.Text.RegularExpressions;
using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Menus;

/// <summary>A heading and body in both languages, one section of a managed page.</summary>
public sealed record PageSection(string HeadingEn, string HeadingAr, string BodyEn, string BodyAr);

public sealed record PageDetails(
    string Slug,
    string? EyebrowEn,
    string? EyebrowAr,
    string TitleEn,
    string TitleAr,
    string SummaryEn,
    string SummaryAr,
    IReadOnlyList<PageSection> Sections,
    string? CtaLabelEn,
    string? CtaLabelAr,
    string? CtaHref,
    string Status);

/// <summary>A landing page staff manage, reachable from the storefront menu.</summary>
public class MenuPage : AuditableEntity
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    public string Slug { get; private set; } = string.Empty;
    public string? EyebrowEn { get; private set; }
    public string? EyebrowAr { get; private set; }
    public string TitleEn { get; private set; } = string.Empty;
    public string TitleAr { get; private set; } = string.Empty;
    public string SummaryEn { get; private set; } = string.Empty;
    public string SummaryAr { get; private set; } = string.Empty;

    /// <summary>The sections as JSON: [{headingEn, headingAr, bodyEn, bodyAr}].</summary>
    public string SectionsJson { get; private set; } = "[]";
    public string? CtaLabelEn { get; private set; }
    public string? CtaLabelAr { get; private set; }
    public string? CtaHref { get; private set; }
    public string Status { get; private set; } = "draft";

    private MenuPage()
    {
    }

    public bool IsPublished => Status == "published";

    public static Result<MenuPage> Create(PageDetails details)
    {
        var page = new MenuPage();
        var applied = page.Apply(details);
        return applied.IsError ? applied.Errors : page;
    }

    public Result<Updated> Apply(PageDetails d)
    {
        var errors = new List<Error>();
        var slug = (d.Slug ?? string.Empty).Trim();
        var titleEn = (d.TitleEn ?? string.Empty).Trim();
        var titleAr = (d.TitleAr ?? string.Empty).Trim();
        var summaryEn = (d.SummaryEn ?? string.Empty).Trim();
        var summaryAr = (d.SummaryAr ?? string.Empty).Trim();
        var eyebrowEn = string.IsNullOrWhiteSpace(d.EyebrowEn) ? null : d.EyebrowEn.Trim();
        var eyebrowAr = string.IsNullOrWhiteSpace(d.EyebrowAr) ? null : d.EyebrowAr.Trim();

        if (slug.Length == 0) errors.Add(MenuErrors.Field("slug", "A slug is required."));
        else if (!SlugPattern.IsMatch(slug)) errors.Add(MenuErrors.Field("slug", "Use lowercase letters, numbers and single hyphens."));
        if (titleEn.Length == 0) errors.Add(MenuErrors.Field("title.en", "The English title is required."));
        if (titleAr.Length == 0) errors.Add(MenuErrors.Field("title.ar", "The Arabic title is required."));
        if (summaryEn.Length == 0) errors.Add(MenuErrors.Field("summary.en", "The English summary is required."));
        if (summaryAr.Length == 0) errors.Add(MenuErrors.Field("summary.ar", "The Arabic summary is required."));
        if (d.Status is not ("draft" or "published")) errors.Add(MenuErrors.Field("status", "Choose draft or published."));
        if ((eyebrowEn is null) != (eyebrowAr is null)) errors.Add(MenuErrors.Field("eyebrow", "Give the eyebrow in both languages or in neither."));

        var sections = d.Sections ?? [];
        for (var i = 0; i < sections.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(sections[i].HeadingEn) || string.IsNullOrWhiteSpace(sections[i].HeadingAr))
                errors.Add(MenuErrors.Field($"sections.{i}.heading.en", "Every section needs a heading in both languages."));
        }

        var ctaEn = string.IsNullOrWhiteSpace(d.CtaLabelEn) ? null : d.CtaLabelEn.Trim();
        var ctaAr = string.IsNullOrWhiteSpace(d.CtaLabelAr) ? null : d.CtaLabelAr.Trim();
        var ctaHref = string.IsNullOrWhiteSpace(d.CtaHref) ? null : d.CtaHref.Trim();
        if (ctaEn is not null || ctaAr is not null || ctaHref is not null)
        {
            if (ctaEn is null || ctaAr is null) errors.Add(MenuErrors.Field("cta.label.en", "The call to action needs a label in both languages."));
            if (ctaHref is null) errors.Add(MenuErrors.Field("cta.href", "The call to action needs a link."));
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        Slug = slug;
        EyebrowEn = eyebrowEn;
        EyebrowAr = eyebrowAr;
        TitleEn = titleEn;
        TitleAr = titleAr;
        SummaryEn = summaryEn;
        SummaryAr = summaryAr;
        SectionsJson = System.Text.Json.JsonSerializer.Serialize(sections);
        CtaLabelEn = ctaEn;
        CtaLabelAr = ctaAr;
        CtaHref = ctaHref;
        Status = d.Status;
        return Result.Updated;
    }

    public IReadOnlyList<PageSection> Sections
        => System.Text.Json.JsonSerializer.Deserialize<List<PageSection>>(SectionsJson) ?? [];
}

/// <summary>A menu, edited as one document (label, heading link, columns, links).</summary>
public class Menu : AuditableEntity
{
    public string Key { get; private set; } = string.Empty;

    /// <summary>The whole menu document as JSON.</summary>
    public string DocumentJson { get; private set; } = "{}";

    private Menu()
    {
    }

    public static Menu Create(string key, string documentJson) => new() { Key = key, DocumentJson = documentJson };

    public void Replace(string documentJson) => DocumentJson = documentJson;
}

public static class MenuErrors
{
    public static readonly Error MenuNotFound = Error.NotFound("Menu.NotFound", "That menu does not exist.");
    public static readonly Error PageNotFound = Error.NotFound("Menu.PageNotFound", "That page does not exist.");

    public static Error Field(string field, string message)
        => Error.Validation($"Menu.{field}", message).ForField(field);

    public static readonly Error SlugTaken = Field("slug", "Another page already uses this slug.");

    public static Error PageInUse(string menus) => Error.Conflict("Menu.PageInUse", $"This page is opened by an item in the {menus} menu. Remove that item first.");
}
