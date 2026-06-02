using AdminService.BLL.Models;
using AdminService.BLL.Services;
using AdminService.DAL.Repositories;
using AdminService.Domain.Entities;

namespace AdminService.Tests;

public sealed class AdminAppServiceTests
{
    [Fact]
    public async Task Enqueue_rejects_invalid_priority()
    {
        var service = new AdminAppService(new FakeAdminRepository());

        var request = new CreateModerationQueueItemRequest(
            "ForumService",
            "post",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "spam",
            null,
            "urgent");

        await Assert.ThrowsAsync<ArgumentException>(() => service.EnqueueAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task Enqueue_normalizes_valid_item()
    {
        var service = new AdminAppService(new FakeAdminRepository());
        var targetId = Guid.NewGuid();

        var item = await service.EnqueueAsync(
            new CreateModerationQueueItemRequest(" ForumService ", " Post ", targetId, null, " Spam ", " Details ", " HIGH "),
            CancellationToken.None);

        Assert.Equal("ForumService", item.SourceService);
        Assert.Equal("Post", item.TargetType);
        Assert.Equal(targetId, item.TargetId);
        Assert.Equal("high", item.Priority);
        Assert.Equal("Spam", item.ReportReason);
    }

    [Fact]
    public async Task LogModerationAction_requires_admin_user()
    {
        var service = new AdminAppService(new FakeAdminRepository());

        var request = new CreateModerationActionRequest("hide_content", "ForumService", "post", Guid.NewGuid(), "spam", null);

        await Assert.ThrowsAsync<ArgumentException>(() => service.LogModerationActionAsync(Guid.Empty, request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateSanction_rejects_past_end_date()
    {
        var service = new AdminAppService(new FakeAdminRepository());

        var request = new CreateUserSanctionRequest(Guid.NewGuid(), "mute", DateTimeOffset.UtcNow.AddMinutes(-1), "spam", null);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateSanctionAsync(Guid.NewGuid(), request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateSanction_stores_active_sanction()
    {
        var repository = new FakeAdminRepository();
        var service = new AdminAppService(repository);
        var userId = Guid.NewGuid();

        var sanction = await service.CreateSanctionAsync(
            Guid.NewGuid(),
            new CreateUserSanctionRequest(userId, "BAN", null, "abuse", "Repeated abuse"),
            CancellationToken.None);

        var active = await service.GetActiveSanctionsForUserAsync(userId, CancellationToken.None);

        Assert.Equal(userId, sanction.UserId);
        Assert.Equal("ban", sanction.SanctionType);
        Assert.Single(active);
    }

    [Fact]
    public async Task Assign_and_close_queue_item_update_status()
    {
        var repository = new FakeAdminRepository();
        var service = new AdminAppService(repository);

        var item = await service.EnqueueAsync(
            new CreateModerationQueueItemRequest("ReviewService", "review", Guid.NewGuid(), null, "fake", null),
            CancellationToken.None);

        var adminId = Guid.NewGuid();
        Assert.True(await service.AssignQueueItemAsync(item.Id, new AssignModerationQueueItemRequest(adminId), CancellationToken.None));
        Assert.True(await service.CloseQueueItemAsync(item.Id, new CloseModerationQueueItemRequest(adminId, "handled"), CancellationToken.None));

        var details = await service.GetQueueItemAsync(item.Id, CancellationToken.None);
        Assert.NotNull(details);
        Assert.Equal("closed", details.Item.Status);
        Assert.Equal("handled", details.Item.CloseNotes);
    }

    private sealed class FakeAdminRepository : IAdminRepository
    {
        private readonly List<ModerationQueueItem> _queue = [];
        private readonly List<ModerationAction> _actions = [];
        private readonly List<UserSanction> _sanctions = [];
        private long _nextQueueId = 1;
        private long _nextActionId = 1;
        private long _nextAuditId = 1;

        public Task<ModerationQueueItem> EnqueueAsync(ModerationQueueItem item, CancellationToken ct)
        {
            var created = item.WithId(_nextQueueId++);
            _queue.Add(created);
            return Task.FromResult(created);
        }

        public Task<ModerationQueueItem?> GetQueueItemAsync(long id, CancellationToken ct)
        {
            return Task.FromResult(_queue.SingleOrDefault(x => x.Id == id));
        }

        public Task<IReadOnlyCollection<ModerationQueueItem>> SearchQueueAsync(string? status, string? sourceService, string? priority, int page, int pageSize, CancellationToken ct)
        {
            var items = _queue
                .Where(x => status is null || x.Status == status)
                .Where(x => sourceService is null || x.SourceService == sourceService)
                .Where(x => priority is null || x.Priority == priority)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<ModerationQueueItem>>(items);
        }

        public Task<bool> AssignQueueItemAsync(long id, Guid adminUserId, CancellationToken ct)
        {
            var index = _queue.FindIndex(x => x.Id == id);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            _queue[index] = _queue[index].WithStatus("in_review", adminUserId, "assigned");
            return Task.FromResult(true);
        }

        public Task<bool> CloseQueueItemAsync(long id, Guid adminUserId, string? notes, CancellationToken ct)
        {
            var index = _queue.FindIndex(x => x.Id == id);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            _queue[index] = _queue[index].WithClosed(adminUserId, notes);
            return Task.FromResult(true);
        }

        public Task<ModerationAction> LogModerationActionAsync(ModerationAction action, CancellationToken ct)
        {
            var created = new ModerationAction
            {
                Id = _nextActionId++,
                AdminUserId = action.AdminUserId,
                ActionType = action.ActionType,
                TargetService = action.TargetService,
                TargetType = action.TargetType,
                TargetId = action.TargetId,
                ReasonCode = action.ReasonCode,
                ReasonDetails = action.ReasonDetails,
                Decision = action.Decision,
                MetadataJson = action.MetadataJson,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _actions.Add(created);
            return Task.FromResult(created);
        }

        public Task<IReadOnlyCollection<ModerationAction>> GetActionsForTargetAsync(string targetService, string targetType, Guid targetId, CancellationToken ct)
        {
            var actions = _actions.Where(x => x.TargetService == targetService && x.TargetType == targetType && x.TargetId == targetId).ToArray();
            return Task.FromResult<IReadOnlyCollection<ModerationAction>>(actions);
        }

        public Task<UserSanction> CreateSanctionAsync(UserSanction sanction, CancellationToken ct)
        {
            var created = new UserSanction
            {
                Id = Guid.NewGuid(),
                UserId = sanction.UserId,
                ImposedByAdmin = sanction.ImposedByAdmin,
                SanctionType = sanction.SanctionType,
                Status = "active",
                StartsAt = sanction.StartsAt,
                EndsAt = sanction.EndsAt,
                ReasonCode = sanction.ReasonCode,
                ReasonDetails = sanction.ReasonDetails,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _sanctions.Add(created);
            return Task.FromResult(created);
        }

        public Task<bool> RevokeSanctionAsync(Guid sanctionId, Guid adminUserId, string? reason, CancellationToken ct)
        {
            var index = _sanctions.FindIndex(x => x.Id == sanctionId && x.Status == "active");
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            var current = _sanctions[index];
            _sanctions[index] = new UserSanction
            {
                Id = current.Id,
                UserId = current.UserId,
                ImposedByAdmin = current.ImposedByAdmin,
                SanctionType = current.SanctionType,
                Status = "revoked",
                StartsAt = current.StartsAt,
                EndsAt = current.EndsAt,
                ReasonCode = current.ReasonCode,
                ReasonDetails = current.ReasonDetails,
                CreatedAt = current.CreatedAt,
                UpdatedAt = DateTimeOffset.UtcNow,
                RevokedAt = DateTimeOffset.UtcNow,
                RevokedByAdmin = adminUserId,
                RevokeReason = reason
            };
            return Task.FromResult(true);
        }

        public Task<IReadOnlyCollection<UserSanction>> GetActiveSanctionsForUserAsync(Guid userId, CancellationToken ct)
        {
            var sanctions = _sanctions.Where(x => x.UserId == userId && x.Status == "active").ToArray();
            return Task.FromResult<IReadOnlyCollection<UserSanction>>(sanctions);
        }

        public Task<AdminAuditLog> LogAuditAsync(AdminAuditLog log, CancellationToken ct)
        {
            return Task.FromResult(new AdminAuditLog
            {
                Id = _nextAuditId++,
                AdminUserId = log.AdminUserId,
                Action = log.Action,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
    }
}

file static class ModerationQueueItemTestExtensions
{
    public static ModerationQueueItem WithId(this ModerationQueueItem item, long id) => new()
    {
        Id = id,
        SourceService = item.SourceService,
        TargetType = item.TargetType,
        TargetId = item.TargetId,
        ReportedByUserId = item.ReportedByUserId,
        ReportReason = item.ReportReason,
        ReportDetails = item.ReportDetails,
        Status = item.Status,
        Priority = item.Priority,
        AssignedToAdmin = item.AssignedToAdmin,
        AssignedAt = item.AssignedAt,
        ClosedAt = item.ClosedAt,
        CloseNotes = item.CloseNotes,
        CreatedAt = DateTimeOffset.UtcNow
    };

    public static ModerationQueueItem WithStatus(this ModerationQueueItem item, string status, Guid adminUserId, string? notes) => new()
    {
        Id = item.Id,
        SourceService = item.SourceService,
        TargetType = item.TargetType,
        TargetId = item.TargetId,
        ReportedByUserId = item.ReportedByUserId,
        ReportReason = item.ReportReason,
        ReportDetails = item.ReportDetails,
        Status = status,
        Priority = item.Priority,
        AssignedToAdmin = adminUserId,
        AssignedAt = DateTimeOffset.UtcNow,
        ClosedAt = item.ClosedAt,
        CloseNotes = notes,
        CreatedAt = item.CreatedAt
    };

    public static ModerationQueueItem WithClosed(this ModerationQueueItem item, Guid adminUserId, string? notes) => new()
    {
        Id = item.Id,
        SourceService = item.SourceService,
        TargetType = item.TargetType,
        TargetId = item.TargetId,
        ReportedByUserId = item.ReportedByUserId,
        ReportReason = item.ReportReason,
        ReportDetails = item.ReportDetails,
        Status = "closed",
        Priority = item.Priority,
        AssignedToAdmin = item.AssignedToAdmin ?? adminUserId,
        AssignedAt = item.AssignedAt ?? DateTimeOffset.UtcNow,
        ClosedAt = DateTimeOffset.UtcNow,
        CloseNotes = notes,
        CreatedAt = item.CreatedAt
    };
}
