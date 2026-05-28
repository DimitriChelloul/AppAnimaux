namespace PetService.Domain.Entities;

public sealed class PetPhoto
{
    public Guid Id { get; init; }
    public Guid PetId { get; init; }
    public Guid MediaId { get; init; }
    public string? MediaUrl { get; init; }
    public int DisplayOrder { get; init; }
    public string? Caption { get; init; }
    public bool IsPrimary { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
