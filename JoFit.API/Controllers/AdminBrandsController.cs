using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Brands.Commands;
using JoFit.Application.Features.Brands.Dtos;
using JoFit.Application.Features.Brands.Queries;
using JoFit.Contracts.Requests.Brands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/admin/brands")]
[ApiVersion("1.0")]
public sealed class AdminBrandsController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Catalogue.Read)]
    [ProducesResponseType(typeof(PagedResult<BrandDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of brands (both languages).")]
    [EndpointName("GetAdminBrands")]
    public async Task<IActionResult> GetPaged([FromQuery] GetBrandsPagedRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetBrandsPagedQuery(request.Page, request.PageSize, request.Search, request.SortField, request.SortDir, request.Locale), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("options")]
    [Authorize(Permissions.Catalogue.Read)]
    [ProducesResponseType(typeof(List<BrandDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets every brand for pickers, optionally limited to ids.")]
    [EndpointName("GetAdminBrandOptions")]
    public async Task<IActionResult> GetOptions([FromQuery] long[]? ids, [FromQuery] string? locale, CancellationToken ct)
    {
        var result = await sender.Send(new GetBrandOptionsQuery(ids, locale), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Catalogue.Read)]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a brand by id.")]
    [EndpointName("GetAdminBrandById")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetBrandByIdQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.Catalogue.Write)]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status201Created)]
    [EndpointSummary("Creates a brand.")]
    [EndpointName("CreateBrand")]
    public async Task<IActionResult> Create([FromBody] BrandDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateBrandCommand(ToInput(request)), ct);
        return result.Match(dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto), Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.Catalogue.Write)]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
    [EndpointSummary("Updates a brand.")]
    [EndpointName("UpdateBrand")]
    public async Task<IActionResult> Update(long id, [FromBody] BrandDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateBrandCommand(id, ToInput(request)), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.Catalogue.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a brand that no product uses.")]
    [EndpointName("DeleteBrand")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteBrandCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    private static BrandInput ToInput(BrandDraftRequest r)
        => new(r.Slug, r.Name.En, r.Name.Ar, r.Logo?.Id, r.Logo?.Url, r.Logo?.Alt);
}
