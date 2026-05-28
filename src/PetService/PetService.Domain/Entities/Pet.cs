namespace PetService.Domain.Entities;

public sealed class Pet
{
    public Guid Id { get; init; }
    public Guid OwnerUserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Species { get; init; } = string.Empty;
    public string? Breed { get; init; }
    public string? Sex { get; init; }
    public DateTime? Birthdate { get; init; }
    public decimal? WeightKg { get; init; }
    public string? Color { get; init; }
    public string? MicrochipId { get; init; }
    public string? TattooId { get; init; }
    public bool IsNeutered { get; init; }
    public string? Allergies { get; init; }
    public string? Notes { get; init; }
    public Guid? MainPhotoMediaId { get; init; }
    public string? MainPhotoUrl { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
