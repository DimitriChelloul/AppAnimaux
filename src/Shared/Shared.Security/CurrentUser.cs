namespace Shared.Security;

public sealed record CurrentUser(Guid? UserId, string? Email, IReadOnlyCollection<string> Roles)
{
    public bool IsAuthenticated => UserId.HasValue || !string.IsNullOrWhiteSpace(Email);
    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}