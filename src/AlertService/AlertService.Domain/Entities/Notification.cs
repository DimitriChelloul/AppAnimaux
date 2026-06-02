namespace AlertService.Domain.Entities;

public sealed class Notification
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string? Title { get; init; }
    public string Body { get; init; } = "";
    public string NotificationType { get; init; } = "";
    public string? Data { get; init; }
    public string Priority { get; init; } = "normal";
    public bool IsRead { get; init; }
    public DateTimeOffset? ReadAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
