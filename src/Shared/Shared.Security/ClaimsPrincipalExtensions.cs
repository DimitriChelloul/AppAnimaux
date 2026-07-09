using System.Security.Claims;

namespace Shared.Security;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value ?? principal.FindFirst("email")?.Value;
    }

    public static CurrentUser ToCurrentUser(this ClaimsPrincipal principal)
    {
        var roles = principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        return new CurrentUser(principal.GetUserId(), principal.GetEmail(), roles);
    }
}