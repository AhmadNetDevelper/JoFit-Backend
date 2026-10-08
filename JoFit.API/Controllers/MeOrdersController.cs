using Asp.Versioning;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Customers;
using JoFit.Application.Features.Orders;
using JoFit.Contracts.Requests.Orders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

/// <summary>
/// The signed-in customer's orders, hires, sessions and addresses (same /me route as profile and preferences).
/// The customer always comes from the token, never from the request.
/// </summary>
[Route("api/v{version:apiVersion}/me")]
[ApiVersion("1.0")]
[Authorize(Roles = "customer")]
public sealed class MeOrdersController(ISender sender, ILocalizationService localization) : ApiController
{
    private string Language => localization.GetCurrentLanguage() ?? "ar";

    [HttpGet("overview")]
    [ProducesResponseType(typeof(AccountOverviewDto), StatusCodes.Status200OK)]
    [EndpointSummary("The account home: next session and hire, recent orders and counts.")]
    [EndpointName("GetMyOverview")]
    public async Task<IActionResult> GetOverview(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyOverviewQuery(CurrentUserId, Language), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("orders")]
    [ProducesResponseType(typeof(List<AccountOrderDto>), StatusCodes.Status200OK)]
    [EndpointSummary("The customer's orders, newest first.")]
    [EndpointName("GetMyOrders")]
    public async Task<IActionResult> GetOrders(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyOrdersQuery(CurrentUserId, Language), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpPost("orders")]
    [ProducesResponseType(typeof(PlacedOrderDto), StatusCodes.Status201Created)]
    [EndpointSummary("Places an order from ids. Everything is re-priced here; there is no online payment.")]
    [EndpointName("PlaceOrder")]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest r, CancellationToken ct)
    {
        DateOnly? Date(string? v) => DateOnly.TryParseExact(v, "yyyy-MM-dd", out var d) ? d : null;
        var lines = r.Lines.Select(l => new OrderLineInput(l.Kind, l.ProductId, l.VariantId, l.RentalItemId, l.Unit, Date(l.From), Date(l.To), l.Quantity)).ToList();
        var result = await sender.Send(new PlaceOrderCommand(CurrentUserId, lines, r.Fulfilment, r.AddressId, r.Notes), ct);
        return result.Match(dto => StatusCode(StatusCodes.Status201Created, dto), Problem);
    }

    [HttpGet("orders/{id:long}")]
    [ProducesResponseType(typeof(AccountOrderDto), StatusCodes.Status200OK)]
    [EndpointSummary("One of the customer's orders. Someone else's is not found.")]
    [EndpointName("GetMyOrder")]
    public async Task<IActionResult> GetOrder(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetMyOrderQuery(CurrentUserId, id, Language), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost("orders/{id:long}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Cancels a pending order and its hires.")]
    [EndpointName("CancelMyOrder")]
    public async Task<IActionResult> CancelOrder(long id, CancellationToken ct)
    {
        var result = await sender.Send(new CancelMyOrderCommand(CurrentUserId, id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpGet("rentals")]
    [ProducesResponseType(typeof(List<AccountRentalDto>), StatusCodes.Status200OK)]
    [EndpointSummary("The customer's hires.")]
    [EndpointName("GetMyRentals")]
    public async Task<IActionResult> GetRentals(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyRentalsQuery(CurrentUserId), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("sessions")]
    [ProducesResponseType(typeof(List<AccountSessionDto>), StatusCodes.Status200OK)]
    [EndpointSummary("The customer's coaching sessions.")]
    [EndpointName("GetMySessions")]
    public async Task<IActionResult> GetSessions(CancellationToken ct)
    {
        var result = await sender.Send(new GetMySessionsQuery(CurrentUserId), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpPost("sessions/{id:long}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Cancels an upcoming pending or confirmed session. A reason of at least 3 characters is required.")]
    [EndpointName("CancelMySession")]
    public async Task<IActionResult> CancelSession(long id, [FromBody] CancelSessionRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new CancelMySessionCommand(CurrentUserId, id, r.Reason), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpGet("addresses")]
    [ProducesResponseType(typeof(List<AddressDto>), StatusCodes.Status200OK)]
    [EndpointSummary("The customer's saved addresses.")]
    [EndpointName("GetMyAddresses")]
    public async Task<IActionResult> GetAddresses(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyAddressesQuery(CurrentUserId), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpPost("addresses")]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status201Created)]
    [EndpointSummary("Saves a delivery address.")]
    [EndpointName("AddMyAddress")]
    public async Task<IActionResult> AddAddress([FromBody] AddressRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new AddMyAddressCommand(CurrentUserId, r.Recipient, r.Phone, r.Line1, r.Line2, r.City, r.Notes), ct);
        return result.Match(dto => StatusCode(StatusCodes.Status201Created, dto), Problem);
    }

    [HttpDelete("addresses/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Removes a saved address. Past orders keep their own copy.")]
    [EndpointName("RemoveMyAddress")]
    public async Task<IActionResult> RemoveAddress(long id, CancellationToken ct)
    {
        var result = await sender.Send(new RemoveMyAddressCommand(CurrentUserId, id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
