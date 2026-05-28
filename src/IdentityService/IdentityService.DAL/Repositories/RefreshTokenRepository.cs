using Dapper;
using IdentityService.Domain.Entities;
using Shared.Persistence.Abstractions;

namespace IdentityService.DAL.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbConnectionFactory _db;

    public RefreshTokenRepository(IDbConnectionFactory db) => _db = db;

    public async Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync<RefreshToken>(
            """
            SELECT
                id AS Id,
                user_id AS UserId,
                token_hash AS TokenHash,
                created_at AS CreatedAt,
                expires_at AS ExpiresAt,
                revoked_at AS RevokedAt,
                replaced_by_token AS ReplacedByToken
            FROM refresh_tokens
            WHERE token_hash = @TokenHash
              AND revoked_at IS NULL
              AND expires_at > now()
            """,
            new { TokenHash = tokenHash });
    }

    public async Task<Guid> InsertAsync(Guid userId, string tokenHash, DateTimeOffset expiresAt, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<Guid>(
            """
            INSERT INTO refresh_tokens (user_id, token_hash, expires_at, created_by_ip, user_agent)
            VALUES (@UserId, @TokenHash, @ExpiresAt, CAST(@IpAddress AS inet), @UserAgent)
            RETURNING id
            """,
            new
            {
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt,
                IpAddress = ipAddress,
                UserAgent = userAgent
            });
    }

    public async Task RevokeAsync(Guid id, string reason, string? ipAddress, Guid? replacedByToken, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        await cn.ExecuteAsync(
            """
            UPDATE refresh_tokens
            SET revoked_at = now(),
                revoked_reason = @Reason,
                revoked_by_ip = CAST(@IpAddress AS inet),
                replaced_by_token = @ReplacedByToken
            WHERE id = @Id
              AND revoked_at IS NULL
            """,
            new
            {
                Id = id,
                Reason = reason,
                IpAddress = ipAddress,
                ReplacedByToken = replacedByToken
            });
    }
}
