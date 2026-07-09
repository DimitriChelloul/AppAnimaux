using Shared.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ApiGatewayService.Api.Gateway;

public sealed class JwtValidator
{
    private readonly JwtOptions _options;

    public JwtValidator(IOptions<JwtOptions> options) => _options = options.Value;

    public bool TryValidate(string? authorizationHeader, out CurrentUser user)
    {
        user = new CurrentUser(null, null, Array.Empty<string>());

        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var token = authorizationHeader["Bearer ".Length..].Trim();
        var parts = token.Split('.');
        if (parts.Length != 3 || string.IsNullOrWhiteSpace(_options.SigningKey))
        {
            return false;
        }

        var unsignedToken = $"{parts[0]}.{parts[1]}";
        var expectedSignature = Sign(unsignedToken);
        if (!FixedTimeEquals(expectedSignature, parts[2]))
        {
            return false;
        }

        JwtPayload? payload;
        try
        {
            var json = Encoding.UTF8.GetString(Base64Url.Decode(parts[1]));
            payload = JsonSerializer.Deserialize<JwtPayload>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return false;
        }

        if (payload is null ||
            payload.Iss != _options.Issuer ||
            payload.Aud != _options.Audience ||
            payload.Exp <= DateTimeOffset.UtcNow.ToUnixTimeSeconds() ||
            !Guid.TryParse(payload.Sub, out var userId))
        {
            return false;
        }

        user = new CurrentUser(userId, payload.Email, payload.Role ?? Array.Empty<string>());
        return true;
    }

    private string Sign(string value)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SigningKey));
        return Base64Url.Encode(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.ASCII.GetBytes(left);
        var rightBytes = Encoding.ASCII.GetBytes(right);
        return leftBytes.Length == rightBytes.Length && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private sealed record JwtPayload(string Iss, string Aud, string Sub, string? Email, string[]? Role, long Exp);
}
