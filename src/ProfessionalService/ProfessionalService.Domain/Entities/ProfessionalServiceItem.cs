namespace ProfessionalService.Domain.Entities;

public sealed class ProfessionalServiceItem
{
    public Guid Id { get; init; }
    public Guid ProfessionalId { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? PriceRange { get; init; }
    public int DisplayOrder { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
