using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.RentalDepots.Commands;
using JoFit.Application.Features.RentalDepots.Dtos;
using JoFit.Application.Features.RentalDepots.Queries;
using JoFit.Contracts.Requests.RentalDepots;
using JoFit.Domain.RentalDepots;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/admin/rental-depots")]
[ApiVersion("1.0")]
public sealed class AdminRentalDepotsController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Rental.Read)]
    [ProducesResponseType(typeof(PagedResult<RentalDepotDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of rental depots (both languages).")]
    [EndpointName("GetAdminRentalDepots")]
    public async Task<IActionResult> GetPaged([FromQuery] GetRentalDepotsPagedRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetRentalDepotsPagedQuery(request.Page, request.PageSize, request.Search, request.SortField, request.SortDir, request.Locale), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("options")]
    [Authorize(Permissions.Rental.Read)]
    [ProducesResponseType(typeof(List<RentalDepotDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets every rental depot for pickers, optionally limited to ids.")]
    [EndpointName("GetAdminRentalDepotOptions")]
    public async Task<IActionResult> GetOptions([FromQuery] long[]? ids, [FromQuery] string? locale, CancellationToken ct)
    {
        var result = await sender.Send(new GetRentalDepotOptionsQuery(ids, locale), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Rental.Read)]
    [ProducesResponseType(typeof(RentalDepotDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a rental depot by id.")]
    [EndpointName("GetAdminRentalDepotById")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetRentalDepotByIdQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.Rental.Write)]
    [ProducesResponseType(typeof(RentalDepotDto), StatusCodes.Status201Created)]
    [EndpointSummary("Creates a rental depot.")]
    [EndpointName("CreateRentalDepot")]
    public async Task<IActionResult> Create([FromBody] RentalDepotDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateRentalDepotCommand(ToValues(request)), ct);
        return result.Match(dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto), Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.Rental.Write)]
    [ProducesResponseType(typeof(RentalDepotDto), StatusCodes.Status200OK)]
    [EndpointSummary("Updates a rental depot.")]
    [EndpointName("UpdateRentalDepot")]
    public async Task<IActionResult> Update(long id, [FromBody] RentalDepotDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateRentalDepotCommand(id, ToValues(request)), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.Rental.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a rental depot that nothing uses.")]
    [EndpointName("DeleteRentalDepot")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteRentalDepotCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    private static RentalDepotValues ToValues(RentalDepotDraftRequest r)
        => new(r.Name.En, r.Name.Ar, r.City.En, r.City.Ar, r.CountryCode);
}
