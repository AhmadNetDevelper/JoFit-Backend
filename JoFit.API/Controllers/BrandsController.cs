using Asp.Versioning;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Brands.Dtos;
using JoFit.Application.Features.Brands.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

/// <summary>Public storefront reads. Text is resolved to the requested language.</summary>
[Route("api/v{version:apiVersion}/brands")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class BrandsController(ISender sender, ILocalizationService localizationService) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<PublicBrandDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Lists brands.")]
    [EndpointName("GetBrands")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicBrandsQuery(localizationService.GetCurrentLanguage() ?? "ar"), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(PublicBrandDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a brand by slug.")]
    [EndpointName("GetBrandBySlug")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicBrandBySlugQuery(slug, localizationService.GetCurrentLanguage() ?? "ar"), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}
