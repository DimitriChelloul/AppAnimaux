using IdentityService.Domain.Entities;

namespace IdentityService.DAL.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken ct);
    Task<Guid> InsertAsync(Guid userId, string tokenHash, DateTimeOffset expiresAt, string? ipAddress, string? userAgent, CancellationToken ct);
    Task RevokeAsync(Guid id, string reason, string? ipAddress, Guid? replacedByToken, CancellationToken ct);
}
