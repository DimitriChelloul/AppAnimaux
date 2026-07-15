using PetService.BLL.Models;
using PetService.DAL.Repositories;
using PetService.Domain.Entities;

namespace PetService.BLL.Services;

public sealed class PetAppService : IPetAppService
{
    private readonly IPetRepository _pets;

    public PetAppService(IPetRepository pets) => _pets = pets;

    public Task<IReadOnlyCollection<Pet>> GetMineAsync(Guid ownerUserId, CancellationToken ct)
    {
        return _pets.GetByOwnerAsync(ownerUserId, ct);
    }

    public async Task<PetResponse?> GetAsync(Guid ownerUserId, Guid petId, CancellationToken ct)
    {
        var pet = await _pets.GetByIdAsync(petId, ct);
        if (pet is null || pet.OwnerUserId != ownerUserId)
        {
            return null;
        }

        var photos = await _pets.GetPhotosAsync(petId, ct);
        return new PetResponse(pet, photos);
    }

    public async Task<PetResponse> CreateAsync(Guid ownerUserId, CreatePetRequest request, CancellationToken ct)
    {
        Validate(request.Name, request.Species);
        var id = Guid.NewGuid();

        await _pets.InsertAsync(
            new Pet
            {
                Id = id,
                OwnerUserId = ownerUserId,
                Name = request.Name.Trim(),
                Species = request.Species.Trim().ToLowerInvariant(),
                Breed = NormalizeOptional(request.Breed),
                Sex = NormalizeOptional(request.Sex)?.ToLowerInvariant(),
                Birthdate = request.Birthdate,
                WeightKg = request.WeightKg,
                Color = NormalizeOptional(request.Color),
                MicrochipId = NormalizeOptional(request.MicrochipId),
                TattooId = NormalizeOptional(request.TattooId),
                IsNeutered = request.IsNeutered,
                Allergies = NormalizeOptional(request.Allergies),
                Notes = NormalizeOptional(request.Notes)
            },
            ct);
        return await GetAsync(ownerUserId, id, ct) ?? throw new InvalidOperationException("Pet could not be loaded.");
    }

    public async Task<PetResponse?> UpdateAsync(Guid ownerUserId, Guid petId, UpdatePetRequest request, CancellationToken ct)
    {
        Validate(request.Name, request.Species);

        var updated = await _pets.UpdateAsync(
            new Pet
            {
                Id = petId,
                OwnerUserId = ownerUserId,
                Name = request.Name.Trim(),
                Species = request.Species.Trim().ToLowerInvariant(),
                Breed = NormalizeOptional(request.Breed),
                Sex = NormalizeOptional(request.Sex)?.ToLowerInvariant(),
                Birthdate = request.Birthdate,
                WeightKg = request.WeightKg,
                Color = NormalizeOptional(request.Color),
                MicrochipId = NormalizeOptional(request.MicrochipId),
                TattooId = NormalizeOptional(request.TattooId),
                IsNeutered = request.IsNeutered,
                Allergies = NormalizeOptional(request.Allergies),
                Notes = NormalizeOptional(request.Notes)
            },
            ct);

        if (!updated) return null;
        return await GetAsync(ownerUserId, petId, ct);
    }

    public async Task<bool> DeleteAsync(Guid ownerUserId, Guid petId, CancellationToken ct)
    {
        var deleted = await _pets.DeleteAsync(petId, ownerUserId, ct);
        return deleted;
    }

    public async Task<PetPhoto?> AddPhotoAsync(Guid ownerUserId, Guid petId, PetPhotoRequest request, CancellationToken ct)
    {
        var pet = await _pets.GetByIdAsync(petId, ct);
        if (pet is null || pet.OwnerUserId != ownerUserId)
        {
            return null;
        }

        var photo = await _pets.AddPhotoAsync(petId, request.MediaId, request.MediaUrl, request.DisplayOrder, request.Caption, request.IsPrimary, ct);
        if (request.IsPrimary && !string.IsNullOrWhiteSpace(request.MediaUrl))
        {
            await _pets.SetMainPhotoAsync(petId, ownerUserId, request.MediaId, request.MediaUrl, ct);
        }
        return photo;
    }

    public async Task<bool> SetMainPhotoAsync(Guid ownerUserId, Guid petId, SetMainPhotoRequest request, CancellationToken ct)
    {
        var updated = await _pets.SetMainPhotoAsync(petId, ownerUserId, request.MediaId, request.MediaUrl, ct);
        return updated;
    }

    private static void Validate(string name, string species)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(species);
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
