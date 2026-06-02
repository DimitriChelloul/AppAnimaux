using AdminService.BLL.Models;
using AdminService.DAL.Repositories;
using AdminService.Domain.Entities;

namespace AdminService.BLL.Services;

public sealed class AdminAppService : IAdminAppService
{
    private static readonly HashSet<string> ValidPriorities = new(StringComparer.OrdinalIgnoreCase) { "low", "normal", "high" };
    private static readonly HashSet<string> ValidQueueStatuses = new(StringComparer.OrdinalIgnoreCase) { "open", "in_review", "closed" };
    private static readonly HashSet<string> ValidDecisions = new(StringComparer.OrdinalIgnoreCase) { "applied", "reverted", "failed" };
    private static readonly HashSet<string> ValidSanctionTypes = new(StringComparer.OrdinalIgnoreCase) { "ban", "mute", "limited" };

    private readonly IAdminRepository _admin;

    public AdminAppService(IAdminRepository admin) => _admin = admin;

    public Task<ModerationQueueItem> EnqueueAsync(CreateModerationQueueItemRequest request, CancellationToken ct)
    {
        ValidateTarget(request.SourceService, request.TargetType, request.TargetId);
        var priority = NormalizePriority(request.Priority);

        return _admin.EnqueueAsync(
            new ModerationQueueItem
            {
                SourceService = NormalizeRequired(request.SourceService, "Source service is required."),
                TargetType = NormalizeRequired(request.TargetType, "Target type is required."),
                TargetId = request.TargetId,
                ReportedByUserId = request.ReportedByUserId,
                ReportReason = NormalizeOptional(request.ReportReason),
                ReportDetails = NormalizeOptional(request.ReportDetails),
                Priority = priority
            },
            ct);
    }

    public async Task<ModerationQueueItemDetails?> GetQueueItemAsync(long id, CancellationToken ct)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Queue item id is required.");
        }

        var item = await _admin.GetQueueItemAsync(id, ct);
        if (item is null)
        {
            return null;
        }

        var actions = await _admin.GetActionsForTargetAsync(item.SourceService, item.TargetType, item.TargetId, ct);
        return new ModerationQueueItemDetails(item, actions);
    }

    public Task<IReadOnlyCollection<ModerationQueueItem>> SearchQueueAsync(ModerationQueueSearchRequest request, CancellationToken ct)
    {
        var status = NormalizeOptional(request.Status)?.ToLowerInvariant();
        if (status is not null && !ValidQueueStatuses.Contains(status))
        {
            throw new ArgumentException("Invalid moderation queue status.");
        }

        var priority = NormalizeOptional(request.Priority)?.ToLowerInvariant();
        if (priority is not null && !ValidPriorities.Contains(priority))
        {
            throw new ArgumentException("Invalid moderation queue priority.");
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        return _admin.SearchQueueAsync(status, NormalizeOptional(request.SourceService), priority, page, pageSize, ct);
    }

    public Task<bool> AssignQueueItemAsync(long id, AssignModerationQueueItemRequest request, CancellationToken ct)
    {
        ValidateQueueId(id);
        ValidateAdmin(request.AdminUserId);
        return _admin.AssignQueueItemAsync(id, request.AdminUserId, ct);
    }

    public Task<bool> CloseQueueItemAsync(long id, CloseModerationQueueItemRequest request, CancellationToken ct)
    {
        ValidateQueueId(id);
        ValidateAdmin(request.AdminUserId);
        return _admin.CloseQueueItemAsync(id, request.AdminUserId, NormalizeOptional(request.Notes), ct);
    }

    public Task<ModerationAction> LogModerationActionAsync(Guid adminUserId, CreateModerationActionRequest request, CancellationToken ct)
    {
        ValidateAdmin(adminUserId);
        ValidateTarget(request.TargetService, request.TargetType, request.TargetId);

        var decision = NormalizeOptional(request.Decision)?.ToLowerInvariant() ?? "applied";
        if (!ValidDecisions.Contains(decision))
        {
            throw new ArgumentException("Invalid moderation decision.");
        }

        return _admin.LogModerationActionAsync(
            new ModerationAction
            {
                AdminUserId = adminUserId,
                ActionType = NormalizeRequired(request.ActionType, "Action type is required.").ToLowerInvariant(),
                TargetService = NormalizeRequired(request.TargetService, "Target service is required."),
                TargetType = NormalizeRequired(request.TargetType, "Target type is required.").ToLowerInvariant(),
                TargetId = request.TargetId,
                ReasonCode = NormalizeOptional(request.ReasonCode)?.ToLowerInvariant(),
                ReasonDetails = NormalizeOptional(request.ReasonDetails),
                Decision = decision,
                MetadataJson = NormalizeMetadataJson(request.MetadataJson)
            },
            ct);
    }

    public Task<IReadOnlyCollection<ModerationAction>> GetActionsForTargetAsync(string targetService, string targetType, Guid targetId, CancellationToken ct)
    {
        ValidateTarget(targetService, targetType, targetId);
        return _admin.GetActionsForTargetAsync(targetService.Trim(), targetType.Trim().ToLowerInvariant(), targetId, ct);
    }

    public Task<UserSanction> CreateSanctionAsync(Guid adminUserId, CreateUserSanctionRequest request, CancellationToken ct)
    {
        ValidateAdmin(adminUserId);
        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.");
        }

        var sanctionType = NormalizeRequired(request.SanctionType, "Sanction type is required.").ToLowerInvariant();
        if (!ValidSanctionTypes.Contains(sanctionType))
        {
            throw new ArgumentException("Invalid sanction type.");
        }

        if (request.EndsAt is not null && request.EndsAt <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException("Sanction end date must be in the future.");
        }

        return _admin.CreateSanctionAsync(
            new UserSanction
            {
                UserId = request.UserId,
                ImposedByAdmin = adminUserId,
                SanctionType = sanctionType,
                StartsAt = DateTimeOffset.UtcNow,
                EndsAt = request.EndsAt,
                ReasonCode = NormalizeOptional(request.ReasonCode)?.ToLowerInvariant(),
                ReasonDetails = NormalizeOptional(request.ReasonDetails)
            },
            ct);
    }

    public Task<bool> RevokeSanctionAsync(Guid sanctionId, RevokeUserSanctionRequest request, CancellationToken ct)
    {
        if (sanctionId == Guid.Empty)
        {
            throw new ArgumentException("Sanction id is required.");
        }

        ValidateAdmin(request.AdminUserId);
        return _admin.RevokeSanctionAsync(sanctionId, request.AdminUserId, NormalizeOptional(request.Reason), ct);
    }

    public Task<IReadOnlyCollection<UserSanction>> GetActiveSanctionsForUserAsync(Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.");
        }

        return _admin.GetActiveSanctionsForUserAsync(userId, ct);
    }

    public Task<AdminAuditLog> LogAuditAsync(Guid adminUserId, CreateAdminAuditLogRequest request, CancellationToken ct)
    {
        ValidateAdmin(adminUserId);
        return _admin.LogAuditAsync(
            new AdminAuditLog
            {
                AdminUserId = adminUserId,
                Action = NormalizeRequired(request.Action, "Audit action is required."),
                IpAddress = NormalizeOptional(request.IpAddress),
                UserAgent = NormalizeOptional(request.UserAgent)
            },
            ct);
    }

    private static void ValidateQueueId(long id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Queue item id is required.");
        }
    }

    private static void ValidateAdmin(Guid adminUserId)
    {
        if (adminUserId == Guid.Empty)
        {
            throw new ArgumentException("Admin user id is required.");
        }
    }

    private static void ValidateTarget(string service, string type, Guid id)
    {
        NormalizeRequired(service, "Target service is required.");
        NormalizeRequired(type, "Target type is required.");
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Target id is required.");
        }
    }

    private static string NormalizePriority(string? value)
    {
        var priority = string.IsNullOrWhiteSpace(value) ? "normal" : value.Trim().ToLowerInvariant();
        return ValidPriorities.Contains(priority) ? priority : throw new ArgumentException("Invalid moderation queue priority.");
    }

    private static string NormalizeMetadataJson(string? value) => string.IsNullOrWhiteSpace(value) ? "{}" : value.Trim();

    private static string NormalizeRequired(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
