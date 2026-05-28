namespace IdentityService.Api.Controllers;

public sealed record AuthResponse(
    Guid UserId,
    string Email,
    IReadOnlyCollection<string> Roles,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
