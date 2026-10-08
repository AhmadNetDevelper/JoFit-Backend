using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Sports.Commands;
using JoFit.Application.Features.Sports.Dtos;
using JoFit.Application.Features.Sports.Queries;
using JoFit.Contracts.Requests.Sports;
using JoFit.Domain.Sports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/admin/sports")]
[ApiVersion("1.0")]
public sealed class AdminSportsController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Policy = "AnyStaff")]
    [ProducesResponseType(typeof(PagedResult<SportDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of sports (both languages).")]
    [EndpointName("GetAdminSports")]
    public async Task<IActionResult> GetPaged([FromQuery] GetSportsPagedRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetSportsPagedQuery(request.Page, request.PageSize, request.Search, request.SortField, request.SortDir, request.Locale), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("options")]
    [Authorize(Policy = "AnyStaff")]
    [ProducesResponseType(typeof(List<SportDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets every sport for pickers, optionally limited to ids.")]
    [EndpointName("GetAdminSportOptions")]
    public async Task<IActionResult> GetOptions([FromQuery] long[]? ids, [FromQuery] string? locale, CancellationToken ct)
    {
        var result = await sender.Send(new GetSportOptionsQuery(ids, locale), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Policy = "AnyStaff")]
    [ProducesResponseType(typeof(SportDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a sport by id.")]
    [EndpointName("GetAdminSportById")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetSportByIdQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.Sports.Write)]
    [ProducesResponseType(typeof(SportDto), StatusCodes.Status201Created)]
    [EndpointSummary("Creates a sport.")]
    [EndpointName("CreateSport")]
    public async Task<IActionResult> Create([FromBody] SportDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateSportCommand(ToValues(request)), ct);
        return result.Match(dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto), Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.Sports.Write)]
    [ProducesResponseType(typeof(SportDto), StatusCodes.Status200OK)]
    [EndpointSummary("Updates a sport.")]
    [EndpointName("UpdateSport")]
    public async Task<IActionResult> Update(long id, [FromBody] SportDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateSportCommand(id, ToValues(request)), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("{id:long}/featured")]
    [Authorize(Permissions.Sports.Write)]
    [ProducesResponseType(typeof(SportDto), StatusCodes.Status200OK)]
    [EndpointSummary("Features or un-features a sport.")]
    [EndpointName("SetSportFeatured")]
    public async Task<IActionResult> SetFeatured(long id, [FromBody] SetFeaturedRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new SetSportFeaturedCommand(id, request.IsFeatured), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.Sports.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a sport that nothing uses.")]
    [EndpointName("DeleteSport")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteSportCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    private static SportValues ToValues(SportDraftRequest r)
        => new(r.Slug, r.Name.En, r.Name.Ar, r.Description?.En, r.Description?.Ar, r.Image?.Id, r.Image?.Url, r.Image?.Alt, r.IsFeatured);
}

public sealed record SetFeaturedRequest(bool IsFeatured);
