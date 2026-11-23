using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SesliAsistan.API.Extensions;

namespace SesliAsistan.API.Hubs;

/// <summary>FG-7: Anlık bildirimler için SignalR Hub (/hubs/notifications).</summary>
[Authorize(Policy = "StudentOrAdmin")]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.GetUserId();
        if (userId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.GetUserId();
        if (userId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
        }
        await base.OnDisconnectedAsync(exception);
    }
}
