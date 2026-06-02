using AdminService.Domain.Entities;

namespace AdminService.DAL.Repositories;

public interface IAdminRepository
{
    Task<ModerationQueueItem> EnqueueAsync(ModerationQueueItem item, CancellationToken ct);
    Task<ModerationQueueItem?> GetQueueItemAsync(long id, CancellationToken ct);
    Task<IReadOnlyCollection<ModerationQueueItem>> SearchQueueAsync(string? status, string? sourceService, string? priority, int page, int pageSize, CancellationToken ct);
    Task<bool> AssignQueueItemAsync(long id, Guid adminUserId, CancellationToken ct);
    Task<bool> CloseQueueItemAsync(long id, Guid adminUserId, string? notes, CancellationToken ct);
    Task<ModerationAction> LogModerationActionAsync(ModerationAction action, CancellationToken ct);
    Task<IReadOnlyCollection<ModerationAction>> GetActionsForTargetAsync(string targetService, string targetType, Guid targetId, CancellationToken ct);
    Task<UserSanction> CreateSanctionAsync(UserSanction sanction, CancellationToken ct);
    Task<bool> RevokeSanctionAsync(Guid sanctionId, Guid adminUserId, string? reason, CancellationToken ct);
    Task<IReadOnlyCollection<UserSanction>> GetActiveSanctionsForUserAsync(Guid userId, CancellationToken ct);
    Task<AdminAuditLog> LogAuditAsync(AdminAuditLog log, CancellationToken ct);
}
