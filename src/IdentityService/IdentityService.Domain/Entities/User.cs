namespace IdentityService.Domain.Entities;

public sealed class User
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public string PasswordAlgo { get; init; } = "PBKDF2";
    public bool IsEmailConfirmed { get; init; }
    public string Status { get; init; } = "active";
    public Guid SecurityStamp { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
}
