namespace AdminService.Domain.Entities;

public sealed class UserSanction
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid ImposedByAdmin { get; init; }
    public string SanctionType { get; init; } = "";
    public string Status { get; init; } = "active";
    public DateTimeOffset StartsAt { get; init; }
    public DateTimeOffset? EndsAt { get; init; }
    public string? ReasonCode { get; init; }
    public string? ReasonDetails { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? RevokedAt { get; init; }
    public Guid? RevokedByAdmin { get; init; }
    public string? RevokeReason { get; init; }
}
