using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Countries.Commands.CreateCountry;
using JoFit.Application.Features.Countries.Commands.DeleteCountry;
using JoFit.Application.Features.Countries.Commands.UpdateCountry;
using JoFit.Application.Features.Countries.Dtos;
using JoFit.Application.Features.Countries.Queries.GetAllCountries;
using JoFit.Application.Features.Countries.Queries.GetCountryById;
using JoFit.Application.Features.Countries.Queries.GetCountriesPaged;
using JoFit.Contracts.Requests.Countries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[JoFit.API.OpenApi.SharedApi]
public sealed class CountryController(ISender sender, ILocalizationService localizationService) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Countries.Read)]
    [ProducesResponseType(typeof(PagedResult<CountryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets a paged list of countries.")]
    [EndpointDescription("Supports optional search on the Arabic/English name and filtering by active status.")]
    [EndpointName("GetCountriesPaged")]
    public async Task<IActionResult> GetPaged([FromQuery] GetCountriesPagedRequest request, CancellationToken ct)
    {
        var query = new GetCountriesPagedQuery(request.Page, request.PageSize, request.Search, request.IsActive);
        var result = await sender.Send(query, ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("all")]
    [Authorize(Permissions.Countries.Read)]
    [ProducesResponseType(typeof(List<CountryLookupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets all active countries (Id and localized Name).")]
    [EndpointDescription("Name is returned in Arabic (ar) or English (en) based on the `language` query parameter (e.g. ?language=en), then the Language claim or Accept-Language header, defaulting to Arabic.")]
    [EndpointName("GetAllCountries")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var language = localizationService.GetCurrentLanguage();
        var result = await sender.Send(new GetAllCountriesQuery(language), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Countries.Read)]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets a country by id.")]
    [EndpointName("GetCountryById")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetCountryByIdQuery(id), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.Countries.Create)]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Creates a new country.")]
    [EndpointName("CreateCountry")]
    public async Task<IActionResult> Create([FromBody] CreateCountryRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateCountryCommand(request.NameAr, request.NameEn, request.IsActive), ct);
        return result.Match(
            dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto),
            Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.Countries.Update)]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates an existing country.")]
    [EndpointName("UpdateCountry")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateCountryRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateCountryCommand(id, request.NameAr, request.NameEn, request.IsActive), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.Countries.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Deletes a country (soft delete).")]
    [EndpointName("DeleteCountry")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteCountryCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
