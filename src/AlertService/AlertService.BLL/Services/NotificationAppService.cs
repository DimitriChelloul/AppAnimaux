using AlertService.BLL.Models;
using AlertService.DAL.Repositories;

namespace AlertService.BLL.Services;

public sealed class NotificationAppService : INotificationAppService
{
    private readonly INotificationRepository _notifications;

    public NotificationAppService(INotificationRepository notifications) => _notifications = notifications;

    public async Task<NotificationsResponse> GetMineAsync(Guid userId, bool unreadOnly, int page, int pageSize, CancellationToken ct)
    {
        var items = await _notifications.GetMineAsync(userId, unreadOnly, page, pageSize, ct);
        return new NotificationsResponse(items, items.Count);
    }

    public Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct)
    {
        return _notifications.MarkReadAsync(userId, notificationId, ct);
    }

    public async Task<MarkAllReadResponse> MarkAllReadAsync(Guid userId, CancellationToken ct)
    {
        var count = await _notifications.MarkAllReadAsync(userId, ct);
        return new MarkAllReadResponse(count);
    }
}
