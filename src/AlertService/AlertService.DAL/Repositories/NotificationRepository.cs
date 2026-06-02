using AlertService.Domain.Entities;
using Dapper;
using Shared.Persistence.Abstractions;

namespace AlertService.DAL.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly IDbConnectionFactory _db;

    public NotificationRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Guid> CreateAsync(Guid userId, string? title, string body, string notificationType, string? dataJson, string priority, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<Guid>(
            """
            INSERT INTO notifications (user_id, title, body, notification_type, data, priority)
            VALUES (@UserId, @Title, @Body, @NotificationType, CAST(@DataJson AS jsonb), @Priority)
            RETURNING id
            """,
            new { UserId = userId, Title = title, Body = body, NotificationType = notificationType, DataJson = dataJson ?? "{}", Priority = priority });
    }

    public async Task<IReadOnlyCollection<Notification>> GetMineAsync(Guid userId, bool unreadOnly, int page, int pageSize, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var rows = await cn.QueryAsync<Notification>(
            """
            SELECT id AS Id, user_id AS UserId, title AS Title, body AS Body,
                   notification_type AS NotificationType, data::text AS Data,
                   priority AS Priority, is_read AS IsRead, read_at AS ReadAt,
                   created_at AS CreatedAt
            FROM notifications
            WHERE user_id = @UserId
              AND (@UnreadOnly = false OR is_read = false)
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset
            """,
            new { UserId = userId, UnreadOnly = unreadOnly, PageSize = pageSize, Offset = (page - 1) * pageSize });

        return rows.ToArray();
    }

    public async Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.ExecuteAsync(
            """
            UPDATE notifications
            SET is_read = true,
                read_at = COALESCE(read_at, now())
            WHERE id = @NotificationId
              AND user_id = @UserId
            """,
            new { UserId = userId, NotificationId = notificationId });

        return rows > 0;
    }

    public async Task<int> MarkAllReadAsync(Guid userId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.ExecuteAsync(
            """
            UPDATE notifications
            SET is_read = true,
                read_at = COALESCE(read_at, now())
            WHERE user_id = @UserId
              AND is_read = false
            """,
            new { UserId = userId });
    }
}
