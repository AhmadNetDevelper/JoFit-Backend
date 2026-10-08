using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.CoachSpecialisations.Commands;
using JoFit.Application.Features.CoachSpecialisations.Dtos;
using JoFit.Application.Features.CoachSpecialisations.Queries;
using JoFit.Contracts.Requests.CoachSpecialisations;
using JoFit.Domain.CoachSpecialisations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/admin/coach-specialisations")]
[ApiVersion("1.0")]
public sealed class AdminCoachSpecialisationsController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Coaches.Read)]
    [ProducesResponseType(typeof(PagedResult<CoachSpecialisationDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of coach specialisations (both languages).")]
    [EndpointName("GetAdminCoachSpecialisations")]
    public async Task<IActionResult> GetPaged([FromQuery] GetCoachSpecialisationsPagedRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetCoachSpecialisationsPagedQuery(request.Page, request.PageSize, request.Search, request.SortField, request.SortDir, request.Locale), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("options")]
    [Authorize(Permissions.Coaches.Read)]
    [ProducesResponseType(typeof(List<CoachSpecialisationDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets every coach specialisation for pickers, optionally limited to ids.")]
    [EndpointName("GetAdminCoachSpecialisationOptions")]
    public async Task<IActionResult> GetOptions([FromQuery] long[]? ids, [FromQuery] string? locale, CancellationToken ct)
    {
        var result = await sender.Send(new GetCoachSpecialisationOptionsQuery(ids, locale), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Coaches.Read)]
    [ProducesResponseType(typeof(CoachSpecialisationDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a coach specialisation by id.")]
    [EndpointName("GetAdminCoachSpecialisationById")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetCoachSpecialisationByIdQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.Coaches.Write)]
    [ProducesResponseType(typeof(CoachSpecialisationDto), StatusCodes.Status201Created)]
    [EndpointSummary("Creates a coach specialisation.")]
    [EndpointName("CreateCoachSpecialisation")]
    public async Task<IActionResult> Create([FromBody] CoachSpecialisationDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateCoachSpecialisationCommand(ToValues(request)), ct);
        return result.Match(dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto), Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.Coaches.Write)]
    [ProducesResponseType(typeof(CoachSpecialisationDto), StatusCodes.Status200OK)]
    [EndpointSummary("Updates a coach specialisation.")]
    [EndpointName("UpdateCoachSpecialisation")]
    public async Task<IActionResult> Update(long id, [FromBody] CoachSpecialisationDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateCoachSpecialisationCommand(id, ToValues(request)), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.Coaches.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a coach specialisation that nothing uses.")]
    [EndpointName("DeleteCoachSpecialisation")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteCoachSpecialisationCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    private static CoachSpecialisationValues ToValues(CoachSpecialisationDraftRequest r)
        => new(r.Name.En, r.Name.Ar);
}
