using Dapper;
using PetService.Domain.Entities;
using Shared.Persistence.Abstractions;

namespace PetService.DAL.Repositories;

public sealed class PetRepository : IPetRepository
{
    private readonly IDbConnectionFactory _db;

    public PetRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Guid> InsertAsync(Pet pet, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleAsync<Guid>(
            """
            INSERT INTO pets (
                id, owner_user_id, name, species, breed, sex, birthdate,
                weight_kg, color, microchip_id, tattoo_id, is_neutered,
                allergies, notes
            )
            VALUES (
                @Id, @OwnerUserId, @Name, @Species, @Breed, @Sex, @Birthdate,
                @WeightKg, @Color, @MicrochipId, @TattooId, @IsNeutered,
                @Allergies, @Notes
            )
            RETURNING id
            """,
            pet);
    }

    public async Task<Pet?> GetByIdAsync(Guid petId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        return await cn.QuerySingleOrDefaultAsync<Pet>(
            """
            SELECT
                id AS Id,
                owner_user_id AS OwnerUserId,
                name AS Name,
                species AS Species,
                breed AS Breed,
                sex AS Sex,
                birthdate::timestamp AS Birthdate,
                weight_kg AS WeightKg,
                color AS Color,
                microchip_id AS MicrochipId,
                tattoo_id AS TattooId,
                is_neutered AS IsNeutered,
                allergies AS Allergies,
                notes AS Notes,
                main_photo_media_id AS MainPhotoMediaId,
                main_photo_url AS MainPhotoUrl,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM pets
            WHERE id = @PetId
            """,
            new { PetId = petId });
    }

    public async Task<IReadOnlyCollection<Pet>> GetByOwnerAsync(Guid ownerUserId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var pets = await cn.QueryAsync<Pet>(
            """
            SELECT
                id AS Id,
                owner_user_id AS OwnerUserId,
                name AS Name,
                species AS Species,
                breed AS Breed,
                sex AS Sex,
                birthdate::timestamp AS Birthdate,
                weight_kg AS WeightKg,
                color AS Color,
                microchip_id AS MicrochipId,
                tattoo_id AS TattooId,
                is_neutered AS IsNeutered,
                allergies AS Allergies,
                notes AS Notes,
                main_photo_media_id AS MainPhotoMediaId,
                main_photo_url AS MainPhotoUrl,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM pets
            WHERE owner_user_id = @OwnerUserId
            ORDER BY created_at DESC
            """,
            new { OwnerUserId = ownerUserId });

        return pets.ToArray();
    }

    public async Task<bool> UpdateAsync(Pet pet, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.ExecuteAsync(
            """
            UPDATE pets
            SET name = @Name,
                species = @Species,
                breed = @Breed,
                sex = @Sex,
                birthdate = @Birthdate,
                weight_kg = @WeightKg,
                color = @Color,
                microchip_id = @MicrochipId,
                tattoo_id = @TattooId,
                is_neutered = @IsNeutered,
                allergies = @Allergies,
                notes = @Notes,
                updated_at = now()
            WHERE id = @Id
              AND owner_user_id = @OwnerUserId
            """,
            pet);

        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid petId, Guid ownerUserId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var rows = await cn.ExecuteAsync(
            "DELETE FROM pets WHERE id = @PetId AND owner_user_id = @OwnerUserId",
            new { PetId = petId, OwnerUserId = ownerUserId });

        return rows > 0;
    }

    public async Task<PetPhoto> AddPhotoAsync(Guid petId, Guid mediaId, string? mediaUrl, int displayOrder, string? caption, bool isPrimary, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        using var tx = cn.BeginTransaction();
        if (isPrimary)
        {
            await cn.ExecuteAsync("UPDATE pet_photos SET is_primary = false WHERE pet_id = @PetId", new { PetId = petId }, tx);
        }

        var photo = await cn.QuerySingleAsync<PetPhoto>(
            """
            INSERT INTO pet_photos (pet_id, media_id, media_url, display_order, caption, is_primary)
            VALUES (@PetId, @MediaId, @MediaUrl, @DisplayOrder, @Caption, @IsPrimary)
            ON CONFLICT (pet_id, media_id)
            DO UPDATE SET
                media_url = EXCLUDED.media_url,
                display_order = EXCLUDED.display_order,
                caption = EXCLUDED.caption,
                is_primary = EXCLUDED.is_primary
            RETURNING
                id AS Id,
                pet_id AS PetId,
                media_id AS MediaId,
                media_url AS MediaUrl,
                display_order AS DisplayOrder,
                caption AS Caption,
                is_primary AS IsPrimary,
                created_at AS CreatedAt
            """,
            new { PetId = petId, MediaId = mediaId, MediaUrl = mediaUrl, DisplayOrder = displayOrder, Caption = caption, IsPrimary = isPrimary },
            tx);

        tx.Commit();
        return photo;
    }

    public async Task<IReadOnlyCollection<PetPhoto>> GetPhotosAsync(Guid petId, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        var photos = await cn.QueryAsync<PetPhoto>(
            """
            SELECT
                id AS Id,
                pet_id AS PetId,
                media_id AS MediaId,
                media_url AS MediaUrl,
                display_order AS DisplayOrder,
                caption AS Caption,
                is_primary AS IsPrimary,
                created_at AS CreatedAt
            FROM pet_photos
            WHERE pet_id = @PetId
            ORDER BY display_order, created_at
            """,
            new { PetId = petId });

        return photos.ToArray();
    }

    public async Task<bool> SetMainPhotoAsync(Guid petId, Guid ownerUserId, Guid mediaId, string mediaUrl, CancellationToken ct)
    {
        using var cn = _db.Create();
        cn.Open();

        using var tx = cn.BeginTransaction();
        var updated = await cn.ExecuteAsync(
            """
            UPDATE pets
            SET main_photo_media_id = @MediaId,
                main_photo_url = @MediaUrl,
                updated_at = now()
            WHERE id = @PetId
              AND owner_user_id = @OwnerUserId
            """,
            new { PetId = petId, OwnerUserId = ownerUserId, MediaId = mediaId, MediaUrl = mediaUrl },
            tx);

        if (updated > 0)
        {
            await cn.ExecuteAsync("UPDATE pet_photos SET is_primary = false WHERE pet_id = @PetId", new { PetId = petId }, tx);
            await cn.ExecuteAsync("UPDATE pet_photos SET is_primary = true WHERE pet_id = @PetId AND media_id = @MediaId", new { PetId = petId, MediaId = mediaId }, tx);
        }

        tx.Commit();
        return updated > 0;
    }
}
