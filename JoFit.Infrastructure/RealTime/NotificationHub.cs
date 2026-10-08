using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace JoFit.Infrastructure.RealTime;

[Authorize]
public sealed class NotificationHub : Hub
{
    public const string HubUrl = "/hubs/notifications";

    /// <summary>
    /// The prefix used for role-based SignalR groups.
    /// Group name format: "role_{roleName}"
    /// </summary>
    public const string RoleGroupPrefix = "role_";

    /// <summary>
    /// Gets the SignalR group name for a specific role.
    /// </summary>
    public static string GetRoleGroupName(string roleName) =>
        $"{RoleGroupPrefix}{roleName}";

    public override async Task OnConnectedAsync()
    {
        // Add the user to a SignalR group for each role they belong to.
        // This allows role-targeted notifications via Groups.
        foreach (var role in GetRoles())
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetRoleGroupName(role));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        foreach (var role in GetRoles())
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetRoleGroupName(role));
        }

        await base.OnDisconnectedAsync(exception);
    }

    private IEnumerable<string> GetRoles() =>
        Context.User?.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Where(r => !string.IsNullOrWhiteSpace(r))
        ?? [];
}
