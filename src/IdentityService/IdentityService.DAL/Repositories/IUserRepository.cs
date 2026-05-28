using IdentityService.Domain.Entities;

namespace IdentityService.DAL.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<string>> GetRoleNamesAsync(Guid userId, CancellationToken ct);
    Task<Guid> InsertAsync(string email, string passwordHash, CancellationToken ct);
    Task MarkLoginSucceededAsync(Guid userId, CancellationToken ct);
    Task AddLoginAuditAsync(Guid? userId, string email, bool succeeded, string reason, string? ipAddress, string? userAgent, CancellationToken ct);
}
