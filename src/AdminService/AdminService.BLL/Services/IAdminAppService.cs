using AdminService.BLL.Models;
using AdminService.Domain.Entities;

namespace AdminService.BLL.Services;

public interface IAdminAppService
{
    Task<ModerationQueueItem> EnqueueAsync(CreateModerationQueueItemRequest request, CancellationToken ct);
    Task<ModerationQueueItemDetails?> GetQueueItemAsync(long id, CancellationToken ct);
    Task<IReadOnlyCollection<ModerationQueueItem>> SearchQueueAsync(ModerationQueueSearchRequest request, CancellationToken ct);
    Task<bool> AssignQueueItemAsync(long id, AssignModerationQueueItemRequest request, CancellationToken ct);
    Task<bool> CloseQueueItemAsync(long id, CloseModerationQueueItemRequest request, CancellationToken ct);
    Task<ModerationAction> LogModerationActionAsync(Guid adminUserId, CreateModerationActionRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<ModerationAction>> GetActionsForTargetAsync(string targetService, string targetType, Guid targetId, CancellationToken ct);
    Task<UserSanction> CreateSanctionAsync(Guid adminUserId, CreateUserSanctionRequest request, CancellationToken ct);
    Task<bool> RevokeSanctionAsync(Guid sanctionId, RevokeUserSanctionRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<UserSanction>> GetActiveSanctionsForUserAsync(Guid userId, CancellationToken ct);
    Task<AdminAuditLog> LogAuditAsync(Guid adminUserId, CreateAdminAuditLogRequest request, CancellationToken ct);
}
