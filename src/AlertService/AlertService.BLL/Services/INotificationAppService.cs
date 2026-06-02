using AlertService.BLL.Models;

namespace AlertService.BLL.Services;

public interface INotificationAppService
{
    Task<NotificationsResponse> GetMineAsync(Guid userId, bool unreadOnly, int page, int pageSize, CancellationToken ct);
    Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct);
    Task<MarkAllReadResponse> MarkAllReadAsync(Guid userId, CancellationToken ct);
}
