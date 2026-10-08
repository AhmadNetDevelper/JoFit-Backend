using JoFit.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace JoFit.Infrastructure.RealTime;

public sealed class SignalRNotifier(IHubContext<NotificationHub> hubContext) : IRealTimeNotifier
{
    private readonly IHubContext<NotificationHub> _hubContext = hubContext;

    public Task SendToAllAsync(string eventName, object payload, CancellationToken ct = default) =>
        _hubContext.Clients.All.SendAsync(eventName, payload, ct);

    public Task SendToRoleAsync(string roleName, string eventName, object payload, CancellationToken ct = default) =>
        _hubContext.Clients.Group(NotificationHub.GetRoleGroupName(roleName)).SendAsync(eventName, payload, ct);

    // SignalR resolves user ids from the ClaimTypes.NameIdentifier claim by default.
    public Task SendToUserAsync(long userId, string eventName, object payload, CancellationToken ct = default) =>
        _hubContext.Clients.User(userId.ToString()).SendAsync(eventName, payload, ct);
}
