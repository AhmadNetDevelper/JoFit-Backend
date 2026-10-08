using System.Text.Json;
using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Services;
using JoFit.Application.Features.Articles;
using JoFit.Contracts.Requests.Common;
using JoFit.Contracts.Requests.Courses;
using JoFit.Domain.Articles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

public sealed record ArticleDraftRequest
{
    public string Slug { get; init; } = string.Empty;
    public LocalizedTextRequest Title { get; init; } = new();
    public LocalizedTextRequest Excerpt { get; init; } = new();
    public LocalizedTextRequest? Body { get; init; }
    public long AuthorId { get; init; }
    public List<long> CategoryIds { get; init; } = [];
    public List<long> SportIds { get; init; } = [];
    public List<string> Tags { get; init; } = [];
    public List<long> RelatedArticleIds { get; init; } = [];

    /// <summary>Absent keeps the cover, null clears it, an image replaces it.</summary>
    public JsonElement? CoverImage { get; init; }
}

public sealed record GetAdminArticlesRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string[]? Statuses { get; init; }
    public long[]? CategoryIds { get; init; }
    public long[]? AuthorIds { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
}

public sealed record GetPublicArticlesRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string[]? CategorySlugs { get; init; }
    public string[]? TagSlugs { get; init; }
    public string[]? SportSlugs { get; init; }
    public long? AuthorId { get; init; }
}

[Route("api/v{version:apiVersion}/admin/articles")]
[ApiVersion("1.0")]
public sealed class AdminArticlesController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Content.Read)]
    [ProducesResponseType(typeof(PagedResult<AdminArticleDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of articles (both languages).")]
    [EndpointName("GetAdminArticles")]
    public async Task<IActionResult> GetPaged([FromQuery] GetAdminArticlesRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminArticlesQuery(r.Page, r.PageSize, r.Search, r.Statuses, r.CategoryIds, r.AuthorIds, r.SortField, r.SortDir), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Content.Read)]
    [ProducesResponseType(typeof(AdminArticleDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets an article by id.")]
    [EndpointName("GetAdminArticle")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminArticleQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.Content.Write)]
    [ProducesResponseType(typeof(AdminArticleDto), StatusCodes.Status201Created)]
    [EndpointSummary("Creates an article. It is always a draft.")]
    [EndpointName("CreateAdminArticle")]
    public async Task<IActionResult> Create([FromBody] ArticleDraftRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new CreateArticleCommand(ToInput(r)), ct);
        return result.Match(dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto), Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.Content.Write)]
    [ProducesResponseType(typeof(AdminArticleDto), StatusCodes.Status200OK)]
    [EndpointSummary("Updates an article. A published article must stay complete in every language.")]
    [EndpointName("UpdateAdminArticle")]
    public async Task<IActionResult> Update(long id, [FromBody] ArticleDraftRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateArticleCommand(id, ToInput(r)), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("{id:long}/status")]
    [Authorize(Permissions.Content.Write)]
    [ProducesResponseType(typeof(AdminArticleDto), StatusCodes.Status200OK)]
    [EndpointSummary("Publishes, archives or returns an article to draft. Publishing needs the body in both languages.")]
    [EndpointName("SetAdminArticleStatus")]
    public async Task<IActionResult> SetStatus(long id, [FromBody] SetStatusRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new SetArticleStatusCommand(id, r.Status), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.Content.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes an article.")]
    [EndpointName("DeleteAdminArticle")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteArticleCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    private static ArticleInput ToInput(ArticleDraftRequest r)
    {
        IReadOnlyList<GalleryItem>? cover = null;
        if (r.CoverImage is JsonElement element)
        {
            cover = element.ValueKind == JsonValueKind.Object && element.TryGetProperty("id", out var id)
                ? [new GalleryItem(id.GetString() ?? string.Empty, element.TryGetProperty("alt", out var alt) ? alt.GetString() : null)]
                : [];
        }

        return new ArticleInput(
            new ArticleDetails(r.Slug, r.Title.En, r.Title.Ar, r.Excerpt.En, r.Excerpt.Ar, r.Body?.En, r.Body?.Ar, r.AuthorId, r.Tags, r.RelatedArticleIds),
            r.CategoryIds, r.SportIds, cover);
    }
}

/// <summary>Public blog reads. Only published articles are visible.</summary>
[Route("api/v{version:apiVersion}")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class ArticlesController(ISender sender, ILocalizationService localization) : ApiController
{
    private string Language => localization.GetCurrentLanguage() ?? "ar";

    [HttpGet("articles")]
    [ProducesResponseType(typeof(PagedResult<PublicArticleDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Lists published articles, newest first (page size up to 200).")]
    [EndpointName("GetArticles")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetList([FromQuery] GetPublicArticlesRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicArticlesQuery(Language, r.Page, r.PageSize, r.Search, r.CategorySlugs, r.TagSlugs, r.SportSlugs, r.AuthorId), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("articles/{slug}")]
    [ProducesResponseType(typeof(PublicArticleDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a published article with its body.")]
    [EndpointName("GetArticleBySlug")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicArticleBySlugQuery(slug, Language), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("articles/{id:long}/related")]
    [ProducesResponseType(typeof(List<PublicArticleDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Related articles: hand-picked first, then those sharing a sport. An unknown id answers an empty list.")]
    [EndpointName("GetRelatedArticles")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetRelated(long id, [FromQuery] int limit = 3, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetRelatedArticlesQuery(id, Language, limit), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("article-tags")]
    [ProducesResponseType(typeof(List<ArticleTagDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Tags used by published articles, with counts.")]
    [EndpointName("GetArticleTags")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetTags(CancellationToken ct)
    {
        var result = await sender.Send(new GetArticleTagsQuery(Language), ct);
        return result.Match(items => Ok(items), Problem);
    }
}
