using AdminService.Domain.Entities;

namespace AdminService.BLL.Models;

public sealed record CreateModerationQueueItemRequest(
    string SourceService,
    string TargetType,
    Guid TargetId,
    Guid? ReportedByUserId,
    string? ReportReason,
    string? ReportDetails,
    string Priority = "normal");

public sealed record ModerationQueueSearchRequest(string? Status, string? SourceService, string? Priority, int Page = 1, int PageSize = 20);

public sealed record AssignModerationQueueItemRequest(Guid AdminUserId);

public sealed record CloseModerationQueueItemRequest(Guid AdminUserId, string? Notes);

public sealed record CreateModerationActionRequest(
    string ActionType,
    string TargetService,
    string TargetType,
    Guid TargetId,
    string? ReasonCode,
    string? ReasonDetails,
    string? Decision = "applied",
    string? MetadataJson = null);

public sealed record CreateUserSanctionRequest(
    Guid UserId,
    string SanctionType,
    DateTimeOffset? EndsAt,
    string? ReasonCode,
    string? ReasonDetails);

public sealed record RevokeUserSanctionRequest(Guid AdminUserId, string? Reason);

public sealed record CreateAdminAuditLogRequest(string Action, string? IpAddress, string? UserAgent);

public sealed record ModerationQueueItemDetails(ModerationQueueItem Item, IReadOnlyCollection<ModerationAction> Actions);
