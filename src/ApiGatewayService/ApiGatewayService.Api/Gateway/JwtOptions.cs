namespace ApiGatewayService.Api.Gateway;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "AppAnimaux.Identity";
    public string Audience { get; init; } = "AppAnimaux";
    public string SigningKey { get; init; } = string.Empty;
}
