namespace AdminService.Domain.Entities;

public sealed class AdminAuditLog
{
    public long Id { get; init; }
    public Guid AdminUserId { get; init; }
    public string Action { get; init; } = "";
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
