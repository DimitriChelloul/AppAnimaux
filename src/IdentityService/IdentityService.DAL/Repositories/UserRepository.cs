using Dapper;
using IdentityService.Domain.Entities;
using Shared.Persistence.Abstractions;

namespace IdentityService.DAL.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _db;

    public UserRepository(IDbConnectionFactory db) => _db = db;

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync<User>(
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
            WHERE email = @Email
            """,
            new { Email = email });
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync<User>(
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
            WHERE id = @Id
            """,
            new { Id = id });
    }

    public async Task<IReadOnlyCollection<string>> GetRoleNamesAsync(Guid userId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var roles = await cn.QueryAsync<string>(
            """
            SELECT r.name
            FROM user_roles ur
            INNER JOIN roles r ON r.id = ur.role_id
            WHERE ur.user_id = @UserId
            ORDER BY r.name
            """,
            new { UserId = userId });

        return roles.ToArray();
    }

    public async Task<Guid> InsertAsync(string email, string passwordHash, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<Guid>(
            """
            INSERT INTO users (email, password_hash)
            VALUES (@Email, @PasswordHash)
            RETURNING id
            """,
            new { Email = email, PasswordHash = passwordHash });
    }

    public async Task MarkLoginSucceededAsync(Guid userId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        await cn.ExecuteAsync(
            """
            UPDATE users
            SET last_login_at = now(),
                access_failed_count = 0,
                updated_at = now()
            WHERE id = @UserId
            """,
            new { UserId = userId });
    }

    public async Task AddLoginAuditAsync(Guid? userId, string email, bool succeeded, string reason, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        await cn.ExecuteAsync(
            """
            INSERT INTO login_audit (user_id, email, succeeded, reason, ip_address, user_agent)
            VALUES (@UserId, @Email, @Succeeded, @Reason, CAST(@IpAddress AS inet), @UserAgent)
            """,
            new
            {
                UserId = userId,
                Email = email,
                Succeeded = succeeded,
                Reason = reason,
                IpAddress = ipAddress,
                UserAgent = userAgent
            });
    }
}
