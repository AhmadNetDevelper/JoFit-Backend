using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.Application.Features.Settings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

public sealed record ThemeTypographyRequest(string? FontFamily, string? Scale);

public sealed record ThemeShapeRequest(string? Skew);

public sealed record ThemeLayoutRequest(string? HeaderMode);

public sealed record ThemeDraftRequest
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Dictionary<string, string> Colors { get; init; } = [];
    public ThemeTypographyRequest Typography { get; init; } = new(null, null);
    public ThemeShapeRequest Shape { get; init; } = new((string?)null);
    public ThemeLayoutRequest Layout { get; init; } = new((string?)null);
}

/// <summary>The storefront reads the theme staff saved in Settings.</summary>
[Route("api/v{version:apiVersion}/theme")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class ThemeController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(StorefrontThemeDto), StatusCodes.Status200OK)]
    [EndpointSummary("The saved storefront theme (colors, typography, shape, layout).")]
    [EndpointName("GetTheme")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicThemeQuery(), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}

[Route("api/v{version:apiVersion}/admin/settings/theme")]
[ApiVersion("1.0")]
public sealed class AdminThemeController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Policy = "AnyStaff")]
    [ProducesResponseType(typeof(StorefrontThemeDto), StatusCodes.Status200OK)]
    [EndpointSummary("The theme, for the settings page and its preview. Any staff member may read it.")]
    [EndpointName("GetAdminTheme")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminThemeQuery(), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut]
    [Authorize(Permissions.Settings.Write)]
    [ProducesResponseType(typeof(StorefrontThemeDto), StatusCodes.Status200OK)]
    [EndpointSummary("Saves the theme. Colours must be hex, four text/background pairs need a contrast of 4.5, and the font must include an Arabic font.")]
    [EndpointName("SaveAdminTheme")]
    public async Task<IActionResult> Save([FromBody] ThemeDraftRequest r, CancellationToken ct)
    {
        var doc = new ThemeDoc(
            r.Id, r.Name, r.Colors,
            new ThemeTypography(r.Typography.FontFamily ?? string.Empty, r.Typography.Scale ?? string.Empty),
            new ThemeShape(r.Shape.Skew ?? string.Empty),
            new ThemeLayout(r.Layout.HeaderMode ?? string.Empty));
        var result = await sender.Send(new SaveThemeCommand(doc), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost("reset")]
    [Authorize(Permissions.Settings.Write)]
    [ProducesResponseType(typeof(StorefrontThemeDto), StatusCodes.Status200OK)]
    [EndpointSummary("Restores the default theme.")]
    [EndpointName("ResetAdminTheme")]
    public async Task<IActionResult> Reset(CancellationToken ct)
    {
        var result = await sender.Send(new ResetThemeCommand(), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}
