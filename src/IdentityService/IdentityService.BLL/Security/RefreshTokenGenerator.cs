using Shared.Security;
using System.Security.Cryptography;
using System.Text;

namespace IdentityService.BLL.Security;

public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    public string Generate() => Base64Url.Encode(RandomNumberGenerator.GetBytes(64));

    public string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
