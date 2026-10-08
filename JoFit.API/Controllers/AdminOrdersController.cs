using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Customers;
using JoFit.Application.Features.Orders;
using JoFit.Contracts.Requests.Courses;
using JoFit.Contracts.Requests.Orders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/admin/orders")]
[ApiVersion("1.0")]
public sealed class AdminOrdersController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Orders.Read)]
    [ProducesResponseType(typeof(PagedResult<AdminOrderDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of orders. Filters: statuses, customerId, placedBetween.from/to.")]
    [EndpointName("GetAdminOrders")]
    public async Task<IActionResult> GetPaged([FromQuery] GetAdminOrdersRequest r, CancellationToken ct)
    {
        DateOnly? Date(string key) => DateOnly.TryParseExact(Request.Query[key].FirstOrDefault(), "yyyy-MM-dd", out var d) ? d : null;
        var result = await sender.Send(
            new GetAdminOrdersQuery(r.Page, r.PageSize, r.Search, r.Statuses, r.CustomerId, Date("placedBetween.from"), Date("placedBetween.to"), r.SortField, r.SortDir), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Orders.Read)]
    [ProducesResponseType(typeof(AdminOrderDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets an order.")]
    [EndpointName("GetAdminOrder")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminOrderQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("{id:long}/status")]
    [Authorize(Permissions.Orders.Write)]
    [ProducesResponseType(typeof(AdminOrderDto), StatusCodes.Status200OK)]
    [EndpointSummary("Moves an order along its status graph. Confirming takes stock; cancelling a held order returns it and releases its hires.")]
    [EndpointName("SetAdminOrderStatus")]
    public async Task<IActionResult> SetStatus(long id, [FromBody] SetStatusRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new SetOrderStatusCommand(id, r.Status), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}

[Route("api/v{version:apiVersion}/admin/customers")]
[ApiVersion("1.0")]
public sealed class AdminCustomersController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Customers.Read)]
    [ProducesResponseType(typeof(PagedResult<AdminCustomerDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of customers with their order counts and lifetime value.")]
    [EndpointName("GetAdminCustomers")]
    public async Task<IActionResult> GetPaged([FromQuery] GetAdminCustomersRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminCustomersQuery(r.Page, r.PageSize, r.Search, r.MarketingOptInOnly, r.SortField, r.SortDir), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Customers.Read)]
    [ProducesResponseType(typeof(AdminCustomerDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a customer.")]
    [EndpointName("GetAdminCustomer")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminCustomerQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.Customers.Write)]
    [ProducesResponseType(typeof(AdminCustomerDto), StatusCodes.Status200OK)]
    [EndpointSummary("Updates a customer. Changing the email changes their login.")]
    [EndpointName("UpdateAdminCustomer")]
    public async Task<IActionResult> Update(long id, [FromBody] CustomerDraftRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateAdminCustomerCommand(id, r.Name, r.Email, r.Phone, r.Locale, r.MarketingOptIn), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}
