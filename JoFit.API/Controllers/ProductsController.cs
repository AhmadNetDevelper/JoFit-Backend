using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Services;
using JoFit.Application.Features.Products;
using JoFit.Contracts.Requests.Products;
using JoFit.Domain.Products;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/admin/products")]
[ApiVersion("1.0")]
public sealed class AdminProductsController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Catalogue.Read)]
    [ProducesResponseType(typeof(PagedResult<AdminProductDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of products (both languages, with variants and stock).")]
    [EndpointName("GetAdminProducts")]
    public async Task<IActionResult> GetPaged([FromQuery] GetAdminProductsRequest r, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetAdminProductsQuery(r.Page, r.PageSize, r.Search, r.Statuses, r.CategoryIds, r.BrandIds, r.SportIds, r.StockStatuses, r.LowStockOnly, r.SortField, r.SortDir), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Catalogue.Read)]
    [ProducesResponseType(typeof(AdminProductDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a product by id.")]
    [EndpointName("GetAdminProduct")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminProductQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.Catalogue.Write)]
    [ProducesResponseType(typeof(AdminProductDto), StatusCodes.Status201Created)]
    [EndpointSummary("Creates a product with its first variant.")]
    [EndpointName("CreateAdminProduct")]
    public async Task<IActionResult> Create([FromBody] NewProductDraftRequest r, CancellationToken ct)
    {
        var input = ToInput(r.Title, r.Summary, r.Description, r.BrandId, r.CategoryIds, r.SportIds, r.Status, r.Media);
        var result = await sender.Send(new CreateProductCommand(input, new NewVariantInput(r.Variant.Sku, r.Variant.Price.Amount, r.Variant.StockOnHand)), ct);
        return result.Match(dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto), Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.Catalogue.Write)]
    [ProducesResponseType(typeof(AdminProductDto), StatusCodes.Status200OK)]
    [EndpointSummary("Updates a product. Variants are not part of the body.")]
    [EndpointName("UpdateAdminProduct")]
    public async Task<IActionResult> Update(long id, [FromBody] ProductDraftRequest r, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateProductCommand(id, ToInput(r.Title, r.Summary, r.Description, r.BrandId, r.CategoryIds, r.SportIds, r.Status, r.Media)), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("{id:long}/status")]
    [Authorize(Permissions.Catalogue.Write)]
    [ProducesResponseType(typeof(AdminProductDto), StatusCodes.Status200OK)]
    [EndpointSummary("Publishes, archives or returns a product to draft.")]
    [EndpointName("SetAdminProductStatus")]
    public async Task<IActionResult> SetStatus(long id, [FromBody] JoFit.Contracts.Requests.Courses.SetStatusRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new SetProductStatusCommand(id, r.Status), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost("{id:long}/stock-adjustments")]
    [Authorize(Permissions.Catalogue.Write)]
    [ProducesResponseType(typeof(AdminProductDto), StatusCodes.Status201Created)]
    [EndpointSummary("Adds or removes stock with a reason. The delta is signed.")]
    [EndpointName("AdjustProductStock")]
    public async Task<IActionResult> AdjustStock(long id, [FromBody] StockAdjustmentRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new AdjustStockCommand(id, r.VariantId, r.Delta, r.Reason, CurrentUserId), ct);
        return result.Match(dto => StatusCode(StatusCodes.Status201Created, dto), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.Catalogue.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a product that is on no order.")]
    [EndpointName("DeleteAdminProduct")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteProductCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    private static ProductInput ToInput(
        JoFit.Contracts.Requests.Common.LocalizedTextRequest title,
        JoFit.Contracts.Requests.Common.LocalizedTextRequest summary,
        JoFit.Contracts.Requests.Common.LocalizedTextRequest? description,
        long brandId, List<long> categoryIds, List<long> sportIds, string status,
        List<JoFit.Contracts.Requests.Common.MediaImageRequest>? media)
        => new(
            new ProductDetails(title.En, title.Ar, summary.En, summary.Ar, description?.En, description?.Ar, brandId, status),
            categoryIds,
            sportIds,
            media?.Select(m => new GalleryItem(m.Id, m.Alt)).ToList());
}

/// <summary>Public storefront reads of the shop. Only published products are visible.</summary>
[Route("api/v{version:apiVersion}/products")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class ProductsController(ISender sender, ILocalizationService localization) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PublicProductDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Lists published products with filters, sorting and paging (page size up to 200).")]
    [EndpointName("GetProducts")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetList([FromQuery] GetPublicProductsRequest r, CancellationToken ct)
    {
        long? Number(string key) => long.TryParse(Request.Query[key].FirstOrDefault(), out var n) ? n : null;
        var result = await sender.Send(
            new GetPublicProductsQuery(
                localization.GetCurrentLanguage() ?? "ar", r.Page, r.PageSize, r.Search,
                r.CategorySlugs, r.BrandSlugs, r.SportSlugs, r.StockStatuses, Number("price.min"), Number("price.max"), r.Sort), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(PublicProductDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a published product by slug.")]
    [EndpointName("GetProductBySlug")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicProductBySlugQuery(slug, localization.GetCurrentLanguage() ?? "ar"), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("{id:long}/related")]
    [ProducesResponseType(typeof(List<PublicProductDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Products that share a sport with this one. An unknown id answers an empty list.")]
    [EndpointName("GetRelatedProducts")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetRelated(long id, [FromQuery] int limit = 4, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetRelatedProductsQuery(id, localization.GetCurrentLanguage() ?? "ar", limit), ct);
        return result.Match(items => Ok(items), Problem);
    }
}
