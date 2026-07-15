using IdentityService.Domain.Entities;

namespace IdentityService.DAL.Repositories;

public interface IRegistrationRepository
{
    Task<User> RegisterAsync(
        Guid userId,
        string email,
        string passwordHash,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct);
}
