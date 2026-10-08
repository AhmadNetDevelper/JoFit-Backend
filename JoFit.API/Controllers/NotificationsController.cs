using Asp.Versioning;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

public sealed record MarkReadRequest(List<long>? Ids);

/// <summary>The bell of a customer, coach or employer.</summary>
[Route("api/v{version:apiVersion}/notifications")]
[ApiVersion("1.0")]
[Authorize(Roles = "customer,coach,employer")]
public sealed class NotificationsController(ISender sender, ILocalizationService localization) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(NotificationFeedDto), StatusCodes.Status200OK)]
    [EndpointSummary("The newest 30 notifications in the requested language, and how many are unread.")]
    [EndpointName("GetMyNotifications")]
    [LocalizedEndpoint]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyNotificationsQuery(CurrentUserId, localization.GetCurrentLanguage() ?? "ar"), ct);
        return result.Match(feed => Ok(feed), Problem);
    }

    [HttpPost("read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Marks notifications read. Send {} to mark them all.")]
    [EndpointName("MarkMyNotificationsRead")]
    public async Task<IActionResult> MarkRead([FromBody] MarkReadRequest? request, CancellationToken ct)
    {
        var result = await sender.Send(new MarkMyNotificationsReadCommand(CurrentUserId, request?.Ids), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}

/// <summary>The staff team's bell: shared by the whole team.</summary>
[Route("api/v{version:apiVersion}/admin/notifications")]
[ApiVersion("1.0")]
[Authorize(Policy = "AnyStaff")]
public sealed class AdminNotificationsController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(StaffNotificationFeedDto), StatusCodes.Status200OK)]
    [EndpointSummary("The newest 50 team notifications in both languages, and how many are unread.")]
    [EndpointName("GetStaffNotifications")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await sender.Send(new GetStaffNotificationsQuery(), ct);
        return result.Match(feed => Ok(feed), Problem);
    }

    [HttpPost("read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Marks team notifications read for everyone. Send {} to mark them all.")]
    [EndpointName("MarkStaffNotificationsRead")]
    public async Task<IActionResult> MarkRead([FromBody] MarkReadRequest? request, CancellationToken ct)
    {
        var result = await sender.Send(new MarkStaffNotificationsReadCommand(request?.Ids), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
