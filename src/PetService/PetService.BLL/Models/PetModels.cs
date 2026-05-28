using PetService.Domain.Entities;

namespace PetService.BLL.Models;

public sealed record CreatePetRequest(
    string Name,
    string Species,
    string? Breed,
    string? Sex,
    DateTime? Birthdate,
    decimal? WeightKg,
    string? Color,
    string? MicrochipId,
    string? TattooId,
    bool IsNeutered,
    string? Allergies,
    string? Notes);

public sealed record UpdatePetRequest(
    string Name,
    string Species,
    string? Breed,
    string? Sex,
    DateTime? Birthdate,
    decimal? WeightKg,
    string? Color,
    string? MicrochipId,
    string? TattooId,
    bool IsNeutered,
    string? Allergies,
    string? Notes);

public sealed record PetPhotoRequest(Guid MediaId, string? MediaUrl, int DisplayOrder, string? Caption, bool IsPrimary);

public sealed record SetMainPhotoRequest(Guid MediaId, string MediaUrl);

public sealed record PetResponse(Pet Pet, IReadOnlyCollection<PetPhoto> Photos);
