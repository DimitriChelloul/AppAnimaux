namespace ForumService.Domain.Entities;

public sealed class ForumCategory
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public string Slug { get; init; } = "";
    public bool IsLocked { get; init; }
    public int SortOrder { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
