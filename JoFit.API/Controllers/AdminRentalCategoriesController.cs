using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.RentalCategories.Commands;
using JoFit.Application.Features.RentalCategories.Dtos;
using JoFit.Application.Features.RentalCategories.Queries;
using JoFit.Contracts.Requests.RentalCategories;
using JoFit.Domain.RentalCategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/admin/rental-categories")]
[ApiVersion("1.0")]
public sealed class AdminRentalCategoriesController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Rental.Read)]
    [ProducesResponseType(typeof(PagedResult<RentalCategoryDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of rental categorys (both languages).")]
    [EndpointName("GetAdminRentalCategories")]
    public async Task<IActionResult> GetPaged([FromQuery] GetRentalCategoriesPagedRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetRentalCategoriesPagedQuery(request.Page, request.PageSize, request.Search, request.SortField, request.SortDir, request.Locale), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("options")]
    [Authorize(Permissions.Rental.Read)]
    [ProducesResponseType(typeof(List<RentalCategoryDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets every rental category for pickers, optionally limited to ids.")]
    [EndpointName("GetAdminRentalCategoryOptions")]
    public async Task<IActionResult> GetOptions([FromQuery] long[]? ids, [FromQuery] string? locale, CancellationToken ct)
    {
        var result = await sender.Send(new GetRentalCategoryOptionsQuery(ids, locale), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Rental.Read)]
    [ProducesResponseType(typeof(RentalCategoryDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a rental category by id.")]
    [EndpointName("GetAdminRentalCategoryById")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetRentalCategoryByIdQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.Rental.Write)]
    [ProducesResponseType(typeof(RentalCategoryDto), StatusCodes.Status201Created)]
    [EndpointSummary("Creates a rental category.")]
    [EndpointName("CreateRentalCategory")]
    public async Task<IActionResult> Create([FromBody] RentalCategoryDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateRentalCategoryCommand(ToValues(request)), ct);
        return result.Match(dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto), Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.Rental.Write)]
    [ProducesResponseType(typeof(RentalCategoryDto), StatusCodes.Status200OK)]
    [EndpointSummary("Updates a rental category.")]
    [EndpointName("UpdateRentalCategory")]
    public async Task<IActionResult> Update(long id, [FromBody] RentalCategoryDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateRentalCategoryCommand(id, ToValues(request)), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.Rental.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a rental category that nothing uses.")]
    [EndpointName("DeleteRentalCategory")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteRentalCategoryCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    private static RentalCategoryValues ToValues(RentalCategoryDraftRequest r)
        => new(r.Slug, r.Name.En, r.Name.Ar, r.Description?.En, r.Description?.Ar, r.Image?.Id, r.Image?.Url, r.Image?.Alt, r.ParentId);
}
