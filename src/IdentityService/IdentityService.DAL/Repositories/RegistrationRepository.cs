using Dapper;
using IdentityService.Domain.Entities;
using Shared.Persistence.Abstractions;

namespace IdentityService.DAL.Repositories;

public sealed class RegistrationRepository : IRegistrationRepository
{
    private readonly IDbConnectionFactory _db;

    public RegistrationRepository(IDbConnectionFactory db) => _db = db;

    public async Task<User> RegisterAsync(
        Guid userId,
        string email,
        string passwordHash,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();
        using var tx = cn.BeginTransaction();

        try
        {
            await cn.ExecuteAsync(
                """
                INSERT INTO users (id, email, password_hash)
                VALUES (@UserId, @Email, @PasswordHash)
                """,
                new { UserId = userId, Email = email, PasswordHash = passwordHash },
                tx);

            await cn.ExecuteAsync(
                """
                INSERT INTO login_audit (user_id, email, succeeded, reason, ip_address, user_agent)
                VALUES (@UserId, @Email, true, 'registered', CAST(@IpAddress AS inet), @UserAgent)
                """,
                new
                {
                    UserId = userId,
                    Email = email,
                    IpAddress = ipAddress,
                    UserAgent = userAgent
                },
                tx);

            var user = await cn.QuerySingleAsync<User>(
                """
                SELECT
                    id AS Id,
                    email AS Email,
                    password_hash AS PasswordHash,
                    password_algo AS PasswordAlgo,
                    is_email_confirmed AS IsEmailConfirmed,
                    status AS Status,
                    security_stamp AS SecurityStamp,
                    created_at AS CreatedAt,
                    last_login_at AS LastLoginAt
                FROM users
                WHERE id = @UserId
                """,
                new { UserId = userId },
                tx);

            tx.Commit();
            return user;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
