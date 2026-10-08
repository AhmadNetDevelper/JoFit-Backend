using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Services;
using JoFit.Application.Features.Rental;
using JoFit.Contracts.Requests.Rental;
using JoFit.Domain.Rental;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/admin/rental-items")]
[ApiVersion("1.0")]
public sealed class AdminRentalItemsController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Rental.Read)]
    [ProducesResponseType(typeof(PagedResult<AdminRentalItemDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of rental items (both languages).")]
    [EndpointName("GetAdminRentalItems")]
    public async Task<IActionResult> GetPaged([FromQuery] GetAdminRentalItemsRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminRentalItemsQuery(r.Page, r.PageSize, r.Search, r.Statuses, r.CategoryIds, r.DepotIds, r.SortField, r.SortDir), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Rental.Read)]
    [ProducesResponseType(typeof(AdminRentalItemDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a rental item by id.")]
    [EndpointName("GetAdminRentalItem")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminRentalItemQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.Rental.Write)]
    [ProducesResponseType(typeof(AdminRentalItemDto), StatusCodes.Status201Created)]
    [EndpointSummary("Creates a rental item.")]
    [EndpointName("CreateAdminRentalItem")]
    public async Task<IActionResult> Create([FromBody] RentalItemDraftRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new CreateRentalItemCommand(ToInput(r)), ct);
        return result.Match(dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto), Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.Rental.Write)]
    [ProducesResponseType(typeof(AdminRentalItemDto), StatusCodes.Status200OK)]
    [EndpointSummary("Updates a rental item, including its status and rates.")]
    [EndpointName("UpdateAdminRentalItem")]
    public async Task<IActionResult> Update(long id, [FromBody] RentalItemDraftRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateRentalItemCommand(id, ToInput(r)), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("{id:long}/out-of-service")]
    [Authorize(Permissions.Rental.Write)]
    [ProducesResponseType(typeof(AdminRentalItemDto), StatusCodes.Status200OK)]
    [EndpointSummary("Sets how many units are out of service (0 up to the total).")]
    [EndpointName("SetRentalUnitsOutOfService")]
    public async Task<IActionResult> SetOutOfService(long id, [FromBody] OutOfServiceRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new SetUnitsOutOfServiceCommand(id, r.UnitsOutOfService), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.Rental.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a rental item with no pending, confirmed or collected hires.")]
    [EndpointName("DeleteAdminRentalItem")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteRentalItemCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    private static RentalItemInput ToInput(RentalItemDraftRequest r)
        => new(
            new RentalItemDetails(
                r.Slug, r.Status, r.Title.En, r.Title.Ar, r.Summary.En, r.Summary.Ar, r.Description?.En, r.Description?.Ar,
                r.Condition, r.DepotId, r.TotalUnits, r.UnitsOutOfService, r.Deposit?.Amount,
                r.Rates.Select(x => new RentalRateInput(x.Unit, x.Price.Amount, x.MinUnits, x.MaxUnits)).ToList()),
            r.CategoryIds,
            r.Media?.Select(m => new GalleryItem(m.Id, m.Alt)).ToList());
}

[Route("api/v{version:apiVersion}/admin/rental-bookings")]
[ApiVersion("1.0")]
public sealed class AdminRentalBookingsController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Rental.Read)]
    [ProducesResponseType(typeof(PagedResult<AdminRentalBookingDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of rental bookings.")]
    [EndpointName("GetAdminRentalBookings")]
    public async Task<IActionResult> GetPaged([FromQuery] GetAdminRentalBookingsRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminRentalBookingsQuery(r.Page, r.PageSize, r.Search, r.Statuses, r.ItemIds, r.CustomerId, r.SortField, r.SortDir), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Rental.Read)]
    [ProducesResponseType(typeof(AdminRentalBookingDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a rental booking.")]
    [EndpointName("GetAdminRentalBooking")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminRentalBookingQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("{id:long}/status")]
    [Authorize(Permissions.Rental.Write)]
    [ProducesResponseType(typeof(AdminRentalBookingDto), StatusCodes.Status200OK)]
    [EndpointSummary("Moves a booking: pending to confirmed or cancelled, confirmed to collected or cancelled, collected to returned.")]
    [EndpointName("SetRentalBookingStatus")]
    public async Task<IActionResult> SetStatus(long id, [FromBody] JoFit.Contracts.Requests.Courses.SetStatusRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new SetRentalBookingStatusCommand(id, r.Status), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost("{id:long}/release-deposit")]
    [Authorize(Permissions.Rental.Write)]
    [ProducesResponseType(typeof(AdminRentalBookingDto), StatusCodes.Status200OK)]
    [EndpointSummary("Releases the deposit of a returned hire.")]
    [EndpointName("ReleaseRentalDeposit")]
    public async Task<IActionResult> ReleaseDeposit(long id, CancellationToken ct)
    {
        var result = await sender.Send(new ReleaseRentalDepositCommand(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}

/// <summary>Public storefront reads of rental. Only published items are visible.</summary>
[Route("api/v{version:apiVersion}/rental-items")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class RentalItemsController(ISender sender, ILocalizationService localization) : ApiController
{
    private static DateOnly? Date(string? value)
        => DateOnly.TryParseExact(value, "yyyy-MM-dd", out var date) ? date : null;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PublicRentalItemDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Lists published rental items (page size up to 200).")]
    [EndpointName("GetRentalItems")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetList([FromQuery] GetPublicRentalItemsRequest r, CancellationToken ct)
    {
        long? Number(string key) => long.TryParse(Request.Query[key].FirstOrDefault(), out var n) ? n : null;
        var result = await sender.Send(
            new GetPublicRentalItemsQuery(
                localization.GetCurrentLanguage() ?? "ar", r.Page, r.PageSize, r.Search, r.CategorySlugs,
                Number("price.min"), Number("price.max"),
                Date(Request.Query["availableBetween.from"].FirstOrDefault()), Date(Request.Query["availableBetween.to"].FirstOrDefault()), r.Sort), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(PublicRentalItemDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a published rental item by slug.")]
    [EndpointName("GetRentalItemBySlug")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicRentalItemBySlugQuery(slug, localization.GetCurrentLanguage() ?? "ar"), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("{id:long}/availability")]
    [ProducesResponseType(typeof(RentalAvailabilityDto), StatusCodes.Status200OK)]
    [EndpointSummary("Free units per day over a range (up to 120 days).")]
    [EndpointName("GetRentalAvailability")]
    public async Task<IActionResult> GetAvailability(long id, [FromQuery] string? from, [FromQuery] string? to, CancellationToken ct)
    {
        var result = await sender.Send(new GetRentalAvailabilityQuery(id, Date(from), Date(to)), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost("{id:long}/quote")]
    [ProducesResponseType(typeof(RentalQuoteDto), StatusCodes.Status200OK)]
    [EndpointSummary("Prices a period. An unavailable or invalid period is a normal answer with isAvailable false.")]
    [EndpointName("QuoteRental")]
    public async Task<IActionResult> Quote(long id, [FromBody] RentalPeriodRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new GetRentalQuoteQuery(id, r.Unit, Date(r.From), Date(r.To), r.Quantity), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}
