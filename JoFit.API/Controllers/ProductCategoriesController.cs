using Asp.Versioning;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.ProductCategories.Dtos;
using JoFit.Application.Features.ProductCategories.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

/// <summary>Public storefront reads. Text is resolved to the requested language.</summary>
[Route("api/v{version:apiVersion}/product-categories")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class ProductCategoriesController(ISender sender, ILocalizationService localizationService) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<PublicProductCategoryDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Lists product categorys.")]
    [EndpointName("GetProductCategories")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicProductCategoriesQuery(localizationService.GetCurrentLanguage() ?? "ar"), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(PublicProductCategoryDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a product category.")]
    [EndpointName("GetProductCategory")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetOne(string slug, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicProductCategoryQuery(slug, localizationService.GetCurrentLanguage() ?? "ar"), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}
