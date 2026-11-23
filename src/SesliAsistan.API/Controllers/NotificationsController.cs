using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesliAsistan.API.Extensions;
using SesliAsistan.Business.DTOs.Social;
using SesliAsistan.Business.Interfaces;

namespace SesliAsistan.API.Controllers;

/// <summary>FG-7: Kullanıcı bildirimleri ve okundu işaretleme.</summary>
[ApiController]
[Authorize(Policy = "StudentOrAdmin")]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications) =>
        _notifications = notifications;

    /// <summary>Bildirimleri listeleme.</summary>
    [HttpGet("api/v1/notifications")]
    [ProducesResponseType<IReadOnlyList<NotificationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> Get(CancellationToken ct) =>
        Ok(await _notifications.GetNotificationsAsync(User.GetUserId(), ct));

    /// <summary>Bildirimleri okundu işaretleme.</summary>
    [HttpPut("api/v1/notifications/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(CancellationToken ct)
    {
        await _notifications.MarkAsReadAsync(User.GetUserId(), ct);
        return NoContent();
    }
}
