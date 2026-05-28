using IdentityService.BLL.Models;

namespace IdentityService.BLL.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, string? ipAddress, string? userAgent, CancellationToken ct);
    Task<AuthResult?> LoginAsync(string email, string password, string? ipAddress, string? userAgent, CancellationToken ct);
    Task<AuthResult?> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken ct);
    Task<bool> LogoutAsync(string refreshToken, string? ipAddress, CancellationToken ct);
}
