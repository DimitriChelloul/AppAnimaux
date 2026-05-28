namespace IdentityService.Domain.Entities;

public sealed class Role
{
    public short Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string NormalizedName { get; init; } = string.Empty;
}
