using ProfessionalService.Domain.Entities;

namespace ProfessionalService.DAL.Repositories;

public interface IProfessionalRepository
{
    Task<Guid> UpsertAsync(Professional professional, CancellationToken ct);
    Task<Professional?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Professional?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyCollection<Professional>> SearchAsync(string? category, string? city, double? latitude, double? longitude, double radiusKm, int page, int pageSize, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct);
    Task<ProfessionalServiceItem> AddServiceAsync(Guid professionalId, string serviceName, string? description, string? priceRange, int displayOrder, CancellationToken ct);
    Task<IReadOnlyCollection<ProfessionalServiceItem>> GetServicesAsync(Guid professionalId, CancellationToken ct);
    Task<ProfessionalPhoto> AddPhotoAsync(Guid professionalId, Guid mediaId, string? mediaUrl, int displayOrder, string? caption, bool isPrimary, CancellationToken ct);
    Task<IReadOnlyCollection<ProfessionalPhoto>> GetPhotosAsync(Guid professionalId, CancellationToken ct);
    Task<bool> SetSubscriptionAsync(Guid professionalId, string plan, string status, CancellationToken ct);
    Task<bool> SetVerifiedAsync(Guid professionalId, bool isVerified, CancellationToken ct);
}
