namespace AdminService.Domain.Entities;

public sealed class ModerationQueueItem
{
    public long Id { get; init; }
    public string SourceService { get; init; } = "";
    public string TargetType { get; init; } = "";
    public Guid TargetId { get; init; }
    public Guid? ReportedByUserId { get; init; }
    public string? ReportReason { get; init; }
    public string? ReportDetails { get; init; }
    public string Status { get; init; } = "open";
    public string Priority { get; init; } = "normal";
    public Guid? AssignedToAdmin { get; init; }
    public DateTimeOffset? AssignedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public string? CloseNotes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
