using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.ArticleCategories.Commands;
using JoFit.Application.Features.ArticleCategories.Dtos;
using JoFit.Application.Features.ArticleCategories.Queries;
using JoFit.Contracts.Requests.ArticleCategories;
using JoFit.Domain.ArticleCategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/admin/article-categories")]
[ApiVersion("1.0")]
public sealed class AdminArticleCategoriesController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Content.Read)]
    [ProducesResponseType(typeof(PagedResult<ArticleCategoryDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of article categorys (both languages).")]
    [EndpointName("GetAdminArticleCategories")]
    public async Task<IActionResult> GetPaged([FromQuery] GetArticleCategoriesPagedRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetArticleCategoriesPagedQuery(request.Page, request.PageSize, request.Search, request.SortField, request.SortDir, request.Locale), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("options")]
    [Authorize(Permissions.Content.Read)]
    [ProducesResponseType(typeof(List<ArticleCategoryDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets every article category for pickers, optionally limited to ids.")]
    [EndpointName("GetAdminArticleCategoryOptions")]
    public async Task<IActionResult> GetOptions([FromQuery] long[]? ids, [FromQuery] string? locale, CancellationToken ct)
    {
        var result = await sender.Send(new GetArticleCategoryOptionsQuery(ids, locale), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Content.Read)]
    [ProducesResponseType(typeof(ArticleCategoryDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a article category by id.")]
    [EndpointName("GetAdminArticleCategoryById")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetArticleCategoryByIdQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.Content.Write)]
    [ProducesResponseType(typeof(ArticleCategoryDto), StatusCodes.Status201Created)]
    [EndpointSummary("Creates a article category.")]
    [EndpointName("CreateArticleCategory")]
    public async Task<IActionResult> Create([FromBody] ArticleCategoryDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateArticleCategoryCommand(ToValues(request)), ct);
        return result.Match(dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto), Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.Content.Write)]
    [ProducesResponseType(typeof(ArticleCategoryDto), StatusCodes.Status200OK)]
    [EndpointSummary("Updates a article category.")]
    [EndpointName("UpdateArticleCategory")]
    public async Task<IActionResult> Update(long id, [FromBody] ArticleCategoryDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateArticleCategoryCommand(id, ToValues(request)), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.Content.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a article category that nothing uses.")]
    [EndpointName("DeleteArticleCategory")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteArticleCategoryCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    private static ArticleCategoryValues ToValues(ArticleCategoryDraftRequest r)
        => new(r.Slug, r.Name.En, r.Name.Ar, r.Description?.En, r.Description?.Ar);
}
