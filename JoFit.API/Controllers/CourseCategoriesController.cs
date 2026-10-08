using Asp.Versioning;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.CourseCategories.Dtos;
using JoFit.Application.Features.CourseCategories.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

/// <summary>Public storefront reads. Text is resolved to the requested language.</summary>
[Route("api/v{version:apiVersion}/course-categories")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class CourseCategoriesController(ISender sender, ILocalizationService localizationService) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<PublicCourseCategoryDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Lists course categorys.")]
    [EndpointName("GetCourseCategories")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicCourseCategoriesQuery(localizationService.GetCurrentLanguage() ?? "ar"), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(PublicCourseCategoryDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a course category.")]
    [EndpointName("GetCourseCategory")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetOne(string slug, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicCourseCategoryQuery(slug, localizationService.GetCurrentLanguage() ?? "ar"), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}
