using Asp.Versioning;
using JoFit.Application.Features.Sitemap;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

/// <summary>What the website lists in its sitemap, one area at a time.</summary>
[Route("api/v{version:apiVersion}/sitemap")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class SitemapController(ISender sender) : ApiController
{
    [HttpGet("{area}")]
    [ProducesResponseType(typeof(List<SitemapEntryDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Public slugs of an area: shop, rental, coaches, education, content or careers.")]
    [EndpointName("GetSitemap")]
    public async Task<IActionResult> Get(string area, CancellationToken ct)
    {
        var result = await sender.Send(new GetSitemapQuery(area), ct);
        return result.Match(items => Ok(items), Problem);
    }
}
