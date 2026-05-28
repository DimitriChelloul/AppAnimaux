namespace IdentityService.BLL.Options;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "AppAnimaux.Identity";
    public string Audience { get; init; } = "AppAnimaux";
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 30;
}
