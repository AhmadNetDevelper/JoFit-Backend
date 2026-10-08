using Asp.Versioning;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.RentalCategories.Dtos;
using JoFit.Application.Features.RentalCategories.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

/// <summary>Public storefront reads. Text is resolved to the requested language.</summary>
[Route("api/v{version:apiVersion}/rental-categories")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class RentalCategoriesController(ISender sender, ILocalizationService localizationService) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<PublicRentalCategoryDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Lists rental categorys.")]
    [EndpointName("GetRentalCategories")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicRentalCategoriesQuery(localizationService.GetCurrentLanguage() ?? "ar"), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(PublicRentalCategoryDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a rental category.")]
    [EndpointName("GetRentalCategory")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetOne(string slug, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicRentalCategoryQuery(slug, localizationService.GetCurrentLanguage() ?? "ar"), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}
