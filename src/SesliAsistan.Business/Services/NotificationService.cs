using Microsoft.Extensions.Logging;
using SesliAsistan.Business.DTOs.Social;
using SesliAsistan.Business.Interfaces;
using SesliAsistan.DataAccess.Repositories;
using SesliAsistan.Entities.Entities;

namespace SesliAsistan.Business.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifications;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notifications,
        IUnitOfWork uow,
        ILogger<NotificationService> logger)
    {
        _notifications = notifications;
        _uow = uow;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(Guid userId, CancellationToken ct = default)
    {
        var list = await _notifications.GetByUserAsync(userId, 50, ct);
        return list.Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.IsRead, n.CreatedAt)).ToList();
    }

    public async Task MarkAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        await _notifications.MarkAllAsReadAsync(userId, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task SendNotificationAsync(Guid userId, string title, string message, CancellationToken ct = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            IsRead = false
        };

        await _notifications.AddAsync(notification, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Kullanıcı {UserId} için bildirim kaydedildi: {Title}", userId, title);
    }
}
