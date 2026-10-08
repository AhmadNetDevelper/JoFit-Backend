using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.ProductCategories.Commands;
using JoFit.Application.Features.ProductCategories.Dtos;
using JoFit.Application.Features.ProductCategories.Queries;
using JoFit.Contracts.Requests.ProductCategories;
using JoFit.Domain.ProductCategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/admin/product-categories")]
[ApiVersion("1.0")]
public sealed class AdminProductCategoriesController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Catalogue.Read)]
    [ProducesResponseType(typeof(PagedResult<ProductCategoryDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of product categorys (both languages).")]
    [EndpointName("GetAdminProductCategories")]
    public async Task<IActionResult> GetPaged([FromQuery] GetProductCategoriesPagedRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetProductCategoriesPagedQuery(request.Page, request.PageSize, request.Search, request.SortField, request.SortDir, request.Locale), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("options")]
    [Authorize(Permissions.Catalogue.Read)]
    [ProducesResponseType(typeof(List<ProductCategoryDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets every product category for pickers, optionally limited to ids.")]
    [EndpointName("GetAdminProductCategoryOptions")]
    public async Task<IActionResult> GetOptions([FromQuery] long[]? ids, [FromQuery] string? locale, CancellationToken ct)
    {
        var result = await sender.Send(new GetProductCategoryOptionsQuery(ids, locale), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Catalogue.Read)]
    [ProducesResponseType(typeof(ProductCategoryDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a product category by id.")]
    [EndpointName("GetAdminProductCategoryById")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetProductCategoryByIdQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.Catalogue.Write)]
    [ProducesResponseType(typeof(ProductCategoryDto), StatusCodes.Status201Created)]
    [EndpointSummary("Creates a product category.")]
    [EndpointName("CreateProductCategory")]
    public async Task<IActionResult> Create([FromBody] ProductCategoryDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateProductCategoryCommand(ToValues(request)), ct);
        return result.Match(dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto), Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.Catalogue.Write)]
    [ProducesResponseType(typeof(ProductCategoryDto), StatusCodes.Status200OK)]
    [EndpointSummary("Updates a product category.")]
    [EndpointName("UpdateProductCategory")]
    public async Task<IActionResult> Update(long id, [FromBody] ProductCategoryDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateProductCategoryCommand(id, ToValues(request)), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.Catalogue.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a product category that nothing uses.")]
    [EndpointName("DeleteProductCategory")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteProductCategoryCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    private static ProductCategoryValues ToValues(ProductCategoryDraftRequest r)
        => new(r.Slug, r.Name.En, r.Name.Ar, r.Description?.En, r.Description?.Ar, r.Image?.Id, r.Image?.Url, r.Image?.Alt, r.ParentId);
}
