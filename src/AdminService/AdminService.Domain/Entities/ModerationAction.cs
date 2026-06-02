namespace AdminService.Domain.Entities;

public sealed class ModerationAction
{
    public long Id { get; init; }
    public Guid AdminUserId { get; init; }
    public string ActionType { get; init; } = "";
    public string TargetService { get; init; } = "";
    public string TargetType { get; init; } = "";
    public Guid TargetId { get; init; }
    public string? ReasonCode { get; init; }
    public string? ReasonDetails { get; init; }
    public string Decision { get; init; } = "applied";
    public string? MetadataJson { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
