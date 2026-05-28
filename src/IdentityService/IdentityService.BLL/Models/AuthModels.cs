namespace IdentityService.BLL.Models;

public sealed record AuthResult(
    Guid UserId,
    string Email,
    IReadOnlyCollection<string> Roles,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record AuthUser(Guid Id, string Email, IReadOnlyCollection<string> Roles);
