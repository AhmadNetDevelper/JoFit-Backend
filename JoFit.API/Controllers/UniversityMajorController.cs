using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.UniversityMajors.Commands.CreateUniversityMajor;
using JoFit.Application.Features.UniversityMajors.Commands.DeleteUniversityMajor;
using JoFit.Application.Features.UniversityMajors.Commands.UpdateUniversityMajor;
using JoFit.Application.Features.UniversityMajors.Dtos;
using JoFit.Application.Features.UniversityMajors.Queries.GetAllUniversityMajors;
using JoFit.Application.Features.UniversityMajors.Queries.GetUniversityMajorById;
using JoFit.Application.Features.UniversityMajors.Queries.GetUniversityMajorsPaged;
using JoFit.Contracts.Requests.UniversityMajors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[JoFit.API.OpenApi.SharedApi]
public sealed class UniversityMajorController(ISender sender, ILocalizationService localizationService) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.UniversityMajors.Read)]
    [ProducesResponseType(typeof(PagedResult<UniversityMajorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets a paged list of university majors.")]
    [EndpointDescription("Supports optional search on the Arabic/English name and filtering by active status.")]
    [EndpointName("GetUniversityMajorsPaged")]
    public async Task<IActionResult> GetPaged([FromQuery] GetUniversityMajorsPagedRequest request, CancellationToken ct)
    {
        var query = new GetUniversityMajorsPagedQuery(request.Page, request.PageSize, request.Search, request.IsActive);
        var result = await sender.Send(query, ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("all")]
    [Authorize(Permissions.UniversityMajors.Read)]
    [ProducesResponseType(typeof(List<UniversityMajorLookupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets all active university majors (Id and localized Name).")]
    [EndpointDescription("Name is returned in Arabic (ar) or English (en) based on the `language` query parameter (e.g. ?language=en), then the Language claim or Accept-Language header, defaulting to Arabic.")]
    [EndpointName("GetAllUniversityMajors")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var language = localizationService.GetCurrentLanguage();
        var result = await sender.Send(new GetAllUniversityMajorsQuery(language), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.UniversityMajors.Read)]
    [ProducesResponseType(typeof(UniversityMajorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets a university major by id.")]
    [EndpointName("GetUniversityMajorById")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetUniversityMajorByIdQuery(id), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.UniversityMajors.Create)]
    [ProducesResponseType(typeof(UniversityMajorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Creates a new university major.")]
    [EndpointName("CreateUniversityMajor")]
    public async Task<IActionResult> Create([FromBody] CreateUniversityMajorRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateUniversityMajorCommand(request.NameAr, request.NameEn, request.IsActive), ct);
        return result.Match(
            dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto),
            Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.UniversityMajors.Update)]
    [ProducesResponseType(typeof(UniversityMajorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates an existing university major.")]
    [EndpointName("UpdateUniversityMajor")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateUniversityMajorRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateUniversityMajorCommand(id, request.NameAr, request.NameEn, request.IsActive), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.UniversityMajors.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Deletes a university major (soft delete).")]
    [EndpointName("DeleteUniversityMajor")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteUniversityMajorCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
