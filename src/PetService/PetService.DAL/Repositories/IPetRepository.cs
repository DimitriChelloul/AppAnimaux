using PetService.Domain.Entities;

namespace PetService.DAL.Repositories;

public interface IPetRepository
{
    Task<Guid> InsertAsync(Pet pet, CancellationToken ct);
    Task<Pet?> GetByIdAsync(Guid petId, CancellationToken ct);
    Task<IReadOnlyCollection<Pet>> GetByOwnerAsync(Guid ownerUserId, CancellationToken ct);
    Task<bool> UpdateAsync(Pet pet, CancellationToken ct);
    Task<bool> DeleteAsync(Guid petId, Guid ownerUserId, CancellationToken ct);
    Task<PetPhoto> AddPhotoAsync(Guid petId, Guid mediaId, string? mediaUrl, int displayOrder, string? caption, bool isPrimary, CancellationToken ct);
    Task<IReadOnlyCollection<PetPhoto>> GetPhotosAsync(Guid petId, CancellationToken ct);
    Task<bool> SetMainPhotoAsync(Guid petId, Guid ownerUserId, Guid mediaId, string mediaUrl, CancellationToken ct);
}
