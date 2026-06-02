using AlertService.Domain.Entities;

namespace AlertService.BLL.Models;

public sealed record NotificationsResponse(IReadOnlyCollection<Notification> Items, int Count);

public sealed record MarkAllReadResponse(int UpdatedCount);
