using Asp.Versioning;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.ArticleCategories.Dtos;
using JoFit.Application.Features.ArticleCategories.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

/// <summary>Public storefront reads. Text is resolved to the requested language.</summary>
[Route("api/v{version:apiVersion}/article-categories")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class ArticleCategoriesController(ISender sender, ILocalizationService localizationService) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<PublicArticleCategoryDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Lists article categorys.")]
    [EndpointName("GetArticleCategories")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicArticleCategoriesQuery(localizationService.GetCurrentLanguage() ?? "ar"), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(PublicArticleCategoryDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a article category.")]
    [EndpointName("GetArticleCategory")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetOne(string slug, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicArticleCategoryQuery(slug, localizationService.GetCurrentLanguage() ?? "ar"), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}
