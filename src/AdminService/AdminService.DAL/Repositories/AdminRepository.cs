using AdminService.Domain.Entities;
using Dapper;
using Shared.Persistence.Abstractions;

namespace AdminService.DAL.Repositories;

public sealed class AdminRepository : IAdminRepository
{
    private readonly IDbConnectionFactory _db;

    public AdminRepository(IDbConnectionFactory db) => _db = db;

    public async Task<ModerationQueueItem> EnqueueAsync(ModerationQueueItem item, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<ModerationQueueItem>(
            """
            INSERT INTO moderation_queue (
                source_service, target_type, target_id, reported_by_user_id,
                report_reason, report_details, priority
            )
            VALUES (
                @SourceService, @TargetType, @TargetId, @ReportedByUserId,
                @ReportReason, @ReportDetails, @Priority
            )
            ON CONFLICT (source_service, target_type, target_id)
            WHERE status IN ('open','in_review')
            DO UPDATE SET
                report_reason = COALESCE(EXCLUDED.report_reason, moderation_queue.report_reason),
                report_details = COALESCE(EXCLUDED.report_details, moderation_queue.report_details),
                priority = CASE
                    WHEN moderation_queue.priority = 'high' OR EXCLUDED.priority = 'high' THEN 'high'
                    WHEN moderation_queue.priority = 'normal' OR EXCLUDED.priority = 'normal' THEN 'normal'
                    ELSE 'low'
                END
            RETURNING
                id AS Id, source_service AS SourceService, target_type AS TargetType, target_id AS TargetId,
                reported_by_user_id AS ReportedByUserId, report_reason AS ReportReason,
                report_details AS ReportDetails, status AS Status, priority AS Priority,
                assigned_to_admin AS AssignedToAdmin, assigned_at AS AssignedAt, closed_at AS ClosedAt,
                close_notes AS CloseNotes, created_at AS CreatedAt
            """,
            item);
    }

    public async Task<ModerationQueueItem?> GetQueueItemAsync(long id, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        return await cn.QuerySingleOrDefaultAsync<ModerationQueueItem>($"{QueueSelectSql} WHERE id = @Id", new { Id = id });
    }

    public async Task<IReadOnlyCollection<ModerationQueueItem>> SearchQueueAsync(string? status, string? sourceService, string? priority, int page, int pageSize, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.QueryAsync<ModerationQueueItem>(
            $"""
            {QueueSelectSql}
            WHERE (@Status IS NULL OR status = @Status)
              AND (@SourceService IS NULL OR source_service = @SourceService)
              AND (@Priority IS NULL OR priority = @Priority)
            ORDER BY
                CASE priority WHEN 'high' THEN 0 WHEN 'normal' THEN 1 ELSE 2 END,
                created_at DESC
            LIMIT @PageSize OFFSET @Offset
            """,
            new { Status = status, SourceService = sourceService, Priority = priority, PageSize = pageSize, Offset = (page - 1) * pageSize });

        return rows.ToArray();
    }

    public async Task<bool> AssignQueueItemAsync(long id, Guid adminUserId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        var rows = await cn.ExecuteAsync(
            """
            UPDATE moderation_queue
            SET status = 'in_review', assigned_to_admin = @AdminUserId, assigned_at = now()
            WHERE id = @Id AND status IN ('open','in_review')
            """,
            new { Id = id, AdminUserId = adminUserId });
        return rows > 0;
    }

    public async Task<bool> CloseQueueItemAsync(long id, Guid adminUserId, string? notes, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        var rows = await cn.ExecuteAsync(
            """
            UPDATE moderation_queue
            SET status = 'closed',
                assigned_to_admin = COALESCE(assigned_to_admin, @AdminUserId),
                assigned_at = COALESCE(assigned_at, now()),
                closed_at = now(),
                close_notes = @Notes
            WHERE id = @Id AND status <> 'closed'
            """,
            new { Id = id, AdminUserId = adminUserId, Notes = notes });
        return rows > 0;
    }

    public async Task<ModerationAction> LogModerationActionAsync(ModerationAction action, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        return await cn.QuerySingleAsync<ModerationAction>(
            """
            INSERT INTO moderation_actions (
                admin_user_id, action_type, target_service, target_type, target_id,
                reason_code, reason_details, decision, metadata
            )
            VALUES (
                @AdminUserId, @ActionType, @TargetService, @TargetType, @TargetId,
                @ReasonCode, @ReasonDetails, @Decision, CAST(@MetadataJson AS jsonb)
            )
            RETURNING
                id AS Id, admin_user_id AS AdminUserId, action_type AS ActionType,
                target_service AS TargetService, target_type AS TargetType, target_id AS TargetId,
                reason_code AS ReasonCode, reason_details AS ReasonDetails, decision AS Decision,
                metadata::text AS MetadataJson, created_at AS CreatedAt
            """,
            action);
    }

    public async Task<IReadOnlyCollection<ModerationAction>> GetActionsForTargetAsync(string targetService, string targetType, Guid targetId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        var rows = await cn.QueryAsync<ModerationAction>(
            """
            SELECT
                id AS Id, admin_user_id AS AdminUserId, action_type AS ActionType,
                target_service AS TargetService, target_type AS TargetType, target_id AS TargetId,
                reason_code AS ReasonCode, reason_details AS ReasonDetails, decision AS Decision,
                metadata::text AS MetadataJson, created_at AS CreatedAt
            FROM moderation_actions
            WHERE target_service = @TargetService AND target_type = @TargetType AND target_id = @TargetId
            ORDER BY created_at DESC
            """,
            new { TargetService = targetService, TargetType = targetType, TargetId = targetId });
        return rows.ToArray();
    }

    public async Task<UserSanction> CreateSanctionAsync(UserSanction sanction, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        return await cn.QuerySingleAsync<UserSanction>(
            """
            INSERT INTO user_sanctions (
                user_id, imposed_by_admin, sanction_type, starts_at, ends_at, reason_code, reason_details
            )
            VALUES (
                @UserId, @ImposedByAdmin, @SanctionType, @StartsAt, @EndsAt, @ReasonCode, @ReasonDetails
            )
            RETURNING
                id AS Id, user_id AS UserId, imposed_by_admin AS ImposedByAdmin, sanction_type AS SanctionType,
                status AS Status, starts_at AS StartsAt, ends_at AS EndsAt, reason_code AS ReasonCode,
                reason_details AS ReasonDetails, created_at AS CreatedAt, updated_at AS UpdatedAt,
                revoked_at AS RevokedAt, revoked_by_admin AS RevokedByAdmin, revoke_reason AS RevokeReason
            """,
            sanction);
    }

    public async Task<bool> RevokeSanctionAsync(Guid sanctionId, Guid adminUserId, string? reason, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        var rows = await cn.ExecuteAsync(
            """
            UPDATE user_sanctions
            SET status = 'revoked',
                updated_at = now(),
                revoked_at = now(),
                revoked_by_admin = @AdminUserId,
                revoke_reason = @Reason
            WHERE id = @SanctionId AND status = 'active'
            """,
            new { SanctionId = sanctionId, AdminUserId = adminUserId, Reason = reason });
        return rows > 0;
    }

    public async Task<IReadOnlyCollection<UserSanction>> GetActiveSanctionsForUserAsync(Guid userId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        var rows = await cn.QueryAsync<UserSanction>(
            """
            SELECT
                id AS Id, user_id AS UserId, imposed_by_admin AS ImposedByAdmin, sanction_type AS SanctionType,
                status AS Status, starts_at AS StartsAt, ends_at AS EndsAt, reason_code AS ReasonCode,
                reason_details AS ReasonDetails, created_at AS CreatedAt, updated_at AS UpdatedAt,
                revoked_at AS RevokedAt, revoked_by_admin AS RevokedByAdmin, revoke_reason AS RevokeReason
            FROM user_sanctions
            WHERE user_id = @UserId
              AND status = 'active'
              AND (ends_at IS NULL OR ends_at > now())
            ORDER BY created_at DESC
            """,
            new { UserId = userId });
        return rows.ToArray();
    }

    public async Task<AdminAuditLog> LogAuditAsync(AdminAuditLog log, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        return await cn.QuerySingleAsync<AdminAuditLog>(
            """
            INSERT INTO admin_audit_logs (admin_user_id, action, ip_address, user_agent)
            VALUES (@AdminUserId, @Action, CAST(@IpAddress AS inet), @UserAgent)
            RETURNING id AS Id, admin_user_id AS AdminUserId, action AS Action,
                      ip_address::text AS IpAddress, user_agent AS UserAgent, created_at AS CreatedAt
            """,
            log);
    }

    private const string QueueSelectSql = """
        SELECT
            id AS Id, source_service AS SourceService, target_type AS TargetType, target_id AS TargetId,
            reported_by_user_id AS ReportedByUserId, report_reason AS ReportReason,
            report_details AS ReportDetails, status AS Status, priority AS Priority,
            assigned_to_admin AS AssignedToAdmin, assigned_at AS AssignedAt, closed_at AS ClosedAt,
            close_notes AS CloseNotes, created_at AS CreatedAt
        FROM moderation_queue
        """;
}
