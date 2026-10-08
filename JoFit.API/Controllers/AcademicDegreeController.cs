using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.AcademicDegrees.Commands.CreateAcademicDegree;
using JoFit.Application.Features.AcademicDegrees.Commands.DeleteAcademicDegree;
using JoFit.Application.Features.AcademicDegrees.Commands.UpdateAcademicDegree;
using JoFit.Application.Features.AcademicDegrees.Dtos;
using JoFit.Application.Features.AcademicDegrees.Queries.GetAllAcademicDegrees;
using JoFit.Application.Features.AcademicDegrees.Queries.GetAcademicDegreeById;
using JoFit.Application.Features.AcademicDegrees.Queries.GetAcademicDegreesPaged;
using JoFit.Contracts.Requests.AcademicDegrees;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[JoFit.API.OpenApi.SharedApi]
public sealed class AcademicDegreeController(ISender sender, ILocalizationService localizationService) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.AcademicDegrees.Read)]
    [ProducesResponseType(typeof(PagedResult<AcademicDegreeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets a paged list of academic degrees.")]
    [EndpointDescription("Supports optional search on the Arabic/English name and filtering by active status.")]
    [EndpointName("GetAcademicDegreesPaged")]
    public async Task<IActionResult> GetPaged([FromQuery] GetAcademicDegreesPagedRequest request, CancellationToken ct)
    {
        var query = new GetAcademicDegreesPagedQuery(request.Page, request.PageSize, request.Search, request.IsActive);
        var result = await sender.Send(query, ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("all")]
    [Authorize(Permissions.AcademicDegrees.Read)]
    [ProducesResponseType(typeof(List<AcademicDegreeLookupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets all active academic degrees (Id and localized Name).")]
    [EndpointDescription("Name is returned in Arabic (ar) or English (en) based on the `language` query parameter (e.g. ?language=en), then the Language claim or Accept-Language header, defaulting to Arabic.")]
    [EndpointName("GetAllAcademicDegrees")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var language = localizationService.GetCurrentLanguage();
        var result = await sender.Send(new GetAllAcademicDegreesQuery(language), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.AcademicDegrees.Read)]
    [ProducesResponseType(typeof(AcademicDegreeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets a academic degree by id.")]
    [EndpointName("GetAcademicDegreeById")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetAcademicDegreeByIdQuery(id), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.AcademicDegrees.Create)]
    [ProducesResponseType(typeof(AcademicDegreeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Creates a new academic degree.")]
    [EndpointName("CreateAcademicDegree")]
    public async Task<IActionResult> Create([FromBody] CreateAcademicDegreeRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateAcademicDegreeCommand(request.NameAr, request.NameEn, request.IsActive), ct);
        return result.Match(
            dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto),
            Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.AcademicDegrees.Update)]
    [ProducesResponseType(typeof(AcademicDegreeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates an existing academic degree.")]
    [EndpointName("UpdateAcademicDegree")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateAcademicDegreeRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateAcademicDegreeCommand(id, request.NameAr, request.NameEn, request.IsActive), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.AcademicDegrees.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Deletes a academic degree (soft delete).")]
    [EndpointName("DeleteAcademicDegree")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteAcademicDegreeCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
