using IdentityService.BLL.Models;

namespace IdentityService.BLL.Security;

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(AuthUser user);
}
