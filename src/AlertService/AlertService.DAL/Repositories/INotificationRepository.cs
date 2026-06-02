using AlertService.Domain.Entities;

namespace AlertService.DAL.Repositories;

public interface INotificationRepository
{
    Task<Guid> CreateAsync(Guid userId, string? title, string body, string notificationType, string? dataJson, string priority, CancellationToken ct);
    Task<IReadOnlyCollection<Notification>> GetMineAsync(Guid userId, bool unreadOnly, int page, int pageSize, CancellationToken ct);
    Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct);
    Task<int> MarkAllReadAsync(Guid userId, CancellationToken ct);
}
