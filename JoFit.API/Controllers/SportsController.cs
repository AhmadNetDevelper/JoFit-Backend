using Asp.Versioning;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Sports.Dtos;
using JoFit.Application.Features.Sports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

/// <summary>Public storefront reads. Text is resolved to the requested language.</summary>
[Route("api/v{version:apiVersion}/sports")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class SportsController(ISender sender, ILocalizationService localizationService) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<PublicSportDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Lists sports.")]
    [EndpointName("GetSports")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetAll([FromQuery] long[]? ids, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicSportsQuery(localizationService.GetCurrentLanguage() ?? "ar", ids), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(PublicSportDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a sport.")]
    [EndpointName("GetSport")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetOne(string slug, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicSportQuery(slug, localizationService.GetCurrentLanguage() ?? "ar"), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}
