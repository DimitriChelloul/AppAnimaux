using PetService.BLL.Models;
using PetService.Domain.Entities;

namespace PetService.BLL.Services;

public interface IPetAppService
{
    Task<IReadOnlyCollection<Pet>> GetMineAsync(Guid ownerUserId, CancellationToken ct);
    Task<PetResponse?> GetAsync(Guid ownerUserId, Guid petId, CancellationToken ct);
    Task<PetResponse> CreateAsync(Guid ownerUserId, CreatePetRequest request, CancellationToken ct);
    Task<PetResponse?> UpdateAsync(Guid ownerUserId, Guid petId, UpdatePetRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid ownerUserId, Guid petId, CancellationToken ct);
    Task<PetPhoto?> AddPhotoAsync(Guid ownerUserId, Guid petId, PetPhotoRequest request, CancellationToken ct);
    Task<bool> SetMainPhotoAsync(Guid ownerUserId, Guid petId, SetMainPhotoRequest request, CancellationToken ct);
}
