namespace JoFit.Application.Common.Interfaces;

/// <summary>
/// Abstraction for pushing real-time events to connected clients.
/// </summary>
public interface IRealTimeNotifier
{
    Task SendToAllAsync(string eventName, object payload, CancellationToken ct = default);

    Task SendToRoleAsync(string roleName, string eventName, object payload, CancellationToken ct = default);

    Task SendToUserAsync(long userId, string eventName, object payload, CancellationToken ct = default);
}
