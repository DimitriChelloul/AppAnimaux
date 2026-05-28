using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IdentityService.BLL.Models;
using IdentityService.BLL.Options;
using Microsoft.Extensions.Options;

namespace IdentityService.BLL.Security;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.SigningKey) || Encoding.UTF8.GetByteCount(_options.SigningKey) < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 bytes.");
        }
    }

    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(AuthUser user)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var header = new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        };

        var payload = new Dictionary<string, object>
        {
            ["iss"] = _options.Issuer,
            ["aud"] = _options.Audience,
            ["sub"] = user.Id.ToString(),
            ["email"] = user.Email,
            ["role"] = user.Roles,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = now.ToUnixTimeSeconds(),
            ["exp"] = expiresAt.ToUnixTimeSeconds(),
            ["jti"] = Guid.NewGuid().ToString()
        };

        var encodedHeader = EncodeJson(header);
        var encodedPayload = EncodeJson(payload);
        var unsignedToken = $"{encodedHeader}.{encodedPayload}";
        var signature = Sign(unsignedToken);

        return ($"{unsignedToken}.{signature}", expiresAt);
    }

    private static string EncodeJson(object value)
    {
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return Base64Url.Encode(Encoding.UTF8.GetBytes(json));
    }

    private string Sign(string value)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SigningKey));
        return Base64Url.Encode(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }
}
