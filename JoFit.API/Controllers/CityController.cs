using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Cities.Commands.CreateCity;
using JoFit.Application.Features.Cities.Commands.DeleteCity;
using JoFit.Application.Features.Cities.Commands.UpdateCity;
using JoFit.Application.Features.Cities.Dtos;
using JoFit.Application.Features.Cities.Queries.GetCitiesByCountryId;
using JoFit.Application.Features.Cities.Queries.GetCitiesPaged;
using JoFit.Application.Features.Cities.Queries.GetCityById;
using JoFit.Contracts.Requests.Cities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[JoFit.API.OpenApi.SharedApi]
public sealed class CityController(ISender sender, ILocalizationService localizationService) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Cities.Read)]
    [ProducesResponseType(typeof(PagedResult<CityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets a paged list of cities.")]
    [EndpointDescription("Supports optional search on the Arabic/English name and filtering by active status and country.")]
    [EndpointName("GetCitiesPaged")]
    public async Task<IActionResult> GetPaged([FromQuery] GetCitiesPagedRequest request, CancellationToken ct)
    {
        var query = new GetCitiesPagedQuery(
            request.Page, request.PageSize, request.Search, request.IsActive, request.CountryId);
        var result = await sender.Send(query, ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("by-country/{countryId:long}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CityLookupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets all active cities of a country (Id and localized Name).")]
    [EndpointDescription("Name is returned in Arabic (ar) or English (en) based on the `language` query parameter (e.g. ?language=en), then the Language claim or Accept-Language header, defaulting to Arabic.")]
    [EndpointName("GetCitiesByCountryId")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetByCountryId(long countryId, CancellationToken ct)
    {
        var language = localizationService.GetCurrentLanguage();
        var result = await sender.Send(new GetCitiesByCountryIdQuery(countryId, language), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Cities.Read)]
    [ProducesResponseType(typeof(CityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets a city by id.")]
    [EndpointName("GetCityById")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetCityByIdQuery(id), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.Cities.Create)]
    [ProducesResponseType(typeof(CityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Creates a new city.")]
    [EndpointName("CreateCity")]
    public async Task<IActionResult> Create([FromBody] CreateCityRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateCityCommand(request.NameAr, request.NameEn, request.CountryId, request.IsActive), ct);
        return result.Match(
            dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto),
            Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.Cities.Update)]
    [ProducesResponseType(typeof(CityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates an existing city.")]
    [EndpointName("UpdateCity")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateCityRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateCityCommand(id, request.NameAr, request.NameEn, request.CountryId, request.IsActive), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.Cities.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Deletes a city (soft delete).")]
    [EndpointName("DeleteCity")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteCityCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
