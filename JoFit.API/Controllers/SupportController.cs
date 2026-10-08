using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Reviews;
using JoFit.Application.Features.Support;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

public sealed record SupportSendRequest(string? Body, string? Name, string? Email);

public sealed record SupportReplyRequest(string? Body);

public sealed record SupportStatusRequest(string? Status);

public sealed record GetSupportInboxRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Status { get; init; }
    public string? Search { get; init; }
}

/// <summary>
/// The chat window. A signed-in customer, coach or employer is identified by their token; a guest by the
/// X-Support-Token header issued with their first message. Anyone else gets nothing back.
/// </summary>
[Route("api/v{version:apiVersion}/support/conversation")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class SupportController(ISender sender) : ApiController
{
    private SupportVisitor Visitor()
    {
        long? userId = CurrentAccountRole is not null && User.Identity?.IsAuthenticated == true ? CurrentUserId : null;
        var token = Request.Headers["X-Support-Token"].FirstOrDefault();
        return new SupportVisitor(userId, string.IsNullOrWhiteSpace(token) ? null : token);
    }

    [HttpGet]
    [ProducesResponseType(typeof(SupportThreadDto), StatusCodes.Status200OK)]
    [EndpointSummary("The visitor's latest conversation (or none), its messages and how many staff replies are unread.")]
    [EndpointName("GetSupportConversation")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await sender.Send(new GetSupportThreadQuery(Visitor()), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost("messages")]
    [ProducesResponseType(typeof(SupportSendResultDto), StatusCodes.Status201Created)]
    [EndpointSummary("Sends a message (2 to 2000 characters). A guest's first message needs a name and an email and returns a guestToken.")]
    [EndpointName("SendSupportMessage")]
    public async Task<IActionResult> Send([FromBody] SupportSendRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new SendSupportMessageCommand(Visitor(), r.Body, r.Name, r.Email), ct);
        return result.Match(dto => StatusCode(StatusCodes.Status201Created, dto), Problem);
    }

    [HttpPost("read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Marks staff replies as seen.")]
    [EndpointName("MarkSupportRead")]
    public async Task<IActionResult> MarkRead(CancellationToken ct)
    {
        var result = await sender.Send(new MarkSupportReadCommand(Visitor()), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}

[Route("api/v{version:apiVersion}/admin/support")]
[ApiVersion("1.0")]
public sealed class AdminSupportController(ISender sender) : ApiController
{
    [HttpGet("conversations")]
    [Authorize(Permissions.Support.Reply)]
    [ProducesResponseType(typeof(PagedResult<ConversationSummaryDto>), StatusCodes.Status200OK)]
    [EndpointSummary("The inbox, newest activity first. Filter by status (open or closed) and search by name, email or reference.")]
    [EndpointName("GetSupportInbox")]
    public async Task<IActionResult> GetInbox([FromQuery] GetSupportInboxRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new GetSupportInboxQuery(r.Page, r.PageSize, r.Status, r.Search), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("unread-count")]
    [Authorize(Permissions.Support.Reply)]
    [ProducesResponseType(typeof(CountDto), StatusCodes.Status200OK)]
    [EndpointSummary("How many conversations have unread visitor messages (for the sidebar badge).")]
    [EndpointName("CountUnreadSupport")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
    {
        var result = await sender.Send(new CountUnreadSupportQuery(), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("conversations/{id:long}")]
    [Authorize(Permissions.Support.Reply)]
    [ProducesResponseType(typeof(SupportThreadDto), StatusCodes.Status200OK)]
    [EndpointSummary("Opens a conversation and marks it seen by staff.")]
    [EndpointName("OpenSupportConversation")]
    public async Task<IActionResult> Open(long id, CancellationToken ct)
    {
        var result = await sender.Send(new OpenSupportConversationQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost("conversations/{id:long}/messages")]
    [Authorize(Permissions.Support.Reply)]
    [ProducesResponseType(typeof(SupportMessageDto), StatusCodes.Status201Created)]
    [EndpointSummary("Replies as the signed-in staff member (1 to 2000 characters).")]
    [EndpointName("ReplySupport")]
    public async Task<IActionResult> Reply(long id, [FromBody] SupportReplyRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new ReplySupportCommand(id, CurrentUserId, r.Body), ct);
        return result.Match(dto => StatusCode(StatusCodes.Status201Created, dto), Problem);
    }

    [HttpPut("conversations/{id:long}/status")]
    [Authorize(Permissions.Support.Reply)]
    [ProducesResponseType(typeof(ConversationSummaryDto), StatusCodes.Status200OK)]
    [EndpointSummary("Closes or reopens a conversation. Anything other than closed means open.")]
    [EndpointName("SetSupportStatus")]
    public async Task<IActionResult> SetStatus(long id, [FromBody] SupportStatusRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new SetSupportStatusCommand(id, r.Status), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}
