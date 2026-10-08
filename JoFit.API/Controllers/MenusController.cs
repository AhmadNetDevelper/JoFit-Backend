using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Menus;
using JoFit.Contracts.Requests.Common;
using JoFit.Domain.Menus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

public sealed record PageSectionRequest
{
    public LocalizedTextRequest Heading { get; init; } = new();
    public LocalizedTextRequest Body { get; init; } = new();
}

public sealed record PageCtaRequest
{
    public LocalizedTextRequest Label { get; init; } = new();
    public string? Href { get; init; }
}

public sealed record MenuPageDraftRequest
{
    public string Slug { get; init; } = string.Empty;
    public LocalizedTextRequest? Eyebrow { get; init; }
    public LocalizedTextRequest Title { get; init; } = new();
    public LocalizedTextRequest Summary { get; init; } = new();
    public List<PageSectionRequest> Sections { get; init; } = [];
    public PageCtaRequest? Cta { get; init; }
    public string Status { get; init; } = "draft";
}

public sealed record MenuTargetRequest(string Kind, long? PageId, string? Href);

public sealed record MenuItemRequest
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public LocalizedTextRequest Label { get; init; } = new();
    public bool IsVisible { get; init; } = true;
    public MenuTargetRequest Target { get; init; } = new("link", null, null);
}

public sealed record MenuGroupRequest
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public LocalizedTextRequest Label { get; init; } = new();
    public List<MenuItemRequest> Items { get; init; } = [];
}

public sealed record MenuDraftRequest
{
    public LocalizedTextRequest Label { get; init; } = new();
    public string? Href { get; init; }
    public List<MenuGroupRequest> Groups { get; init; } = [];
}

public sealed record GetMenuPagesRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
}

[Route("api/v{version:apiVersion}/admin/menus")]
[ApiVersion("1.0")]
public sealed class AdminMenusController(ISender sender) : ApiController
{
    [HttpGet("{key}")]
    [Authorize(Permissions.Content.Read)]
    [ProducesResponseType(typeof(AdminMenuDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a menu as one document. An unknown key is not found.")]
    [EndpointName("GetAdminMenu")]
    public async Task<IActionResult> Get(string key, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminMenuQuery(key), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("{key}")]
    [Authorize(Permissions.Content.Write)]
    [ProducesResponseType(typeof(AdminMenuDto), StatusCodes.Status200OK)]
    [EndpointSummary("Replaces a menu (label, heading link, columns and their links) as one document. An unknown key is created.")]
    [EndpointName("SaveAdminMenu")]
    public async Task<IActionResult> Save(string key, [FromBody] MenuDraftRequest r, CancellationToken ct)
    {
        var doc = new MenuDoc(
            new LocalizedText(r.Label.En, r.Label.Ar), r.Href,
            r.Groups.Select(g => new MenuGroupDoc(
                g.Id, new LocalizedText(g.Label.En, g.Label.Ar),
                g.Items.Select(i => new MenuItemDoc(i.Id, new LocalizedText(i.Label.En, i.Label.Ar), i.IsVisible, new MenuTargetDoc(i.Target.Kind, i.Target.PageId, i.Target.Href))).ToList())).ToList());
        var result = await sender.Send(new SaveMenuCommand(key, doc), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}

[Route("api/v{version:apiVersion}/admin/menu-pages")]
[ApiVersion("1.0")]
public sealed class AdminMenuPagesController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Content.Read)]
    [ProducesResponseType(typeof(PagedResult<AdminMenuPageDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of managed pages.")]
    [EndpointName("GetAdminMenuPages")]
    public async Task<IActionResult> GetPaged([FromQuery] GetMenuPagesRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminMenuPagesQuery(r.Page, r.PageSize, r.Search, r.SortField, r.SortDir), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("options")]
    [Authorize(Permissions.Content.Read)]
    [ProducesResponseType(typeof(List<AdminMenuPageDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Every page, for the menu editor's page picker.")]
    [EndpointName("GetAdminMenuPageOptions")]
    public async Task<IActionResult> GetOptions(CancellationToken ct)
    {
        var result = await sender.Send(new GetMenuPageOptionsQuery(), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Content.Read)]
    [ProducesResponseType(typeof(AdminMenuPageDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a managed page.")]
    [EndpointName("GetAdminMenuPage")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminMenuPageQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.Content.Write)]
    [ProducesResponseType(typeof(AdminMenuPageDto), StatusCodes.Status201Created)]
    [EndpointSummary("Creates a managed page.")]
    [EndpointName("CreateAdminMenuPage")]
    public async Task<IActionResult> Create([FromBody] MenuPageDraftRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new SaveMenuPageCommand(null, ToDetails(r)), ct);
        return result.Match(dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto), Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.Content.Write)]
    [ProducesResponseType(typeof(AdminMenuPageDto), StatusCodes.Status200OK)]
    [EndpointSummary("Updates a managed page.")]
    [EndpointName("UpdateAdminMenuPage")]
    public async Task<IActionResult> Update(long id, [FromBody] MenuPageDraftRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new SaveMenuPageCommand(id, ToDetails(r)), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.Content.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a page that no menu item opens.")]
    [EndpointName("DeleteAdminMenuPage")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteMenuPageCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    private static PageDetails ToDetails(MenuPageDraftRequest r)
        => new(
            r.Slug, r.Eyebrow?.En, r.Eyebrow?.Ar, r.Title.En, r.Title.Ar, r.Summary.En, r.Summary.Ar,
            r.Sections.Select(s => new PageSection(s.Heading.En, s.Heading.Ar, s.Body.En, s.Body.Ar)).ToList(),
            r.Cta?.Label.En, r.Cta?.Label.Ar, r.Cta?.Href, r.Status);
}

/// <summary>Public reads of staff-managed menus and pages. Labels are resolved to the requested language.</summary>
[Route("api/v{version:apiVersion}")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class MenusController(ISender sender, ILocalizationService localization) : ApiController
{
    private string Language => localization.GetCurrentLanguage() ?? "ar";

    [HttpGet("menus/{key}")]
    [ProducesResponseType(typeof(PublicMenuDto), StatusCodes.Status200OK)]
    [EndpointSummary("A menu for the storefront: hidden items and unpublished pages are dropped, empty columns go with them.")]
    [EndpointName("GetMenu")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetMenu(string key, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicMenuQuery(key, Language), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("menu-pages")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [EndpointSummary("Slugs of published managed pages.")]
    [EndpointName("GetMenuPageSlugs")]
    public async Task<IActionResult> GetSlugs(CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicMenuPageSlugsQuery(), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("menu-pages/{slug}")]
    [ProducesResponseType(typeof(PublicMenuPageDto), StatusCodes.Status200OK)]
    [EndpointSummary("A published managed page. Anything else is not found.")]
    [EndpointName("GetMenuPage")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetPage(string slug, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicMenuPageQuery(slug, Language), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}
