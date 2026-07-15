-- 01-pet-schema.sql
-- Schéma du PetService (pet_db)
connect pet_db

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "citext";

------------------------------------------------------------
-- Enum (simples via CHECK pour rester portable)
------------------------------------------------------------

------------------------------------------------------------
-- Table pets : fiche animal
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS pets (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),

    owner_user_id   uuid NOT NULL, -- id utilisateur (IdentityService) SANS FK (microservices)

    name            text NOT NULL,
    species         varchar(30) NOT NULL,  -- dog/cat/other...
    breed           text,
    sex             varchar(10),           -- male/female/unknown
    birthdate       date,
    weight_kg       numeric(5,2),

    color           text,
    microchip_id    varchar(64),
    tattoo_id       varchar(64),

    is_neutered     boolean NOT NULL DEFAULT false,

    -- santé / infos utiles
    allergies       text,
    notes           text,

    -- médias : on stocke des IDs/URLs vers MediaService (pas de fichiers ici)
    main_photo_media_id uuid,
    main_photo_url      text,

    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT uq_pets_owner_name UNIQUE (owner_user_id, name)
);

CREATE INDEX IF NOT EXISTS idx_pets_owner_user_id ON pets (owner_user_id);
CREATE INDEX IF NOT EXISTS idx_pets_species ON pets (species);

------------------------------------------------------------
-- Table pet_photos : galerie de photos d'un animal
-- Les fichiers restent dans MediaService ; PetService garde l'ordre
-- et les metadonnees metier de la galerie.
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS pet_photos (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    pet_id          uuid NOT NULL,
    media_id        uuid NOT NULL,
    media_url       text,
    display_order   int NOT NULL DEFAULT 0,
    caption         text,
    is_primary      boolean NOT NULL DEFAULT false,
    created_at      timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_pet_photos_pet
        FOREIGN KEY (pet_id) REFERENCES pets(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_pet_photos_pet_id ON pet_photos (pet_id);
CREATE UNIQUE INDEX IF NOT EXISTS uq_pet_photos_media_id ON pet_photos (pet_id, media_id);
CREATE INDEX IF NOT EXISTS idx_pet_photos_order ON pet_photos (pet_id, display_order);

------------------------------------------------------------
-- Table pet_medical_records : historique médical (simplifié)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS pet_medical_records (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    pet_id          uuid NOT NULL,

    record_type     varchar(50) NOT NULL,  -- consultation/vaccin/operation/traitement/autre
    title           text NOT NULL,
    description     text,
    vet_name        text,
    occurred_on     date,
    attachments     jsonb, -- ex: [{ "mediaId": "...", "url": "..." }]

    created_at      timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_medical_pet
        FOREIGN KEY (pet_id) REFERENCES pets(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_medical_pet_id ON pet_medical_records (pet_id);
CREATE INDEX IF NOT EXISTS idx_medical_occurred_on ON pet_medical_records (occurred_on);

------------------------------------------------------------
-- Table pet_vaccinations : suivi vaccins (optionnel mais utile)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS pet_vaccinations (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    pet_id          uuid NOT NULL,

    vaccine_name    text NOT NULL,         -- rage, CHPPiL, etc.
    vaccinated_on   date NOT NULL,
    valid_until     date,
    vet_name        text,
    notes           text,

    created_at      timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_vaccinations_pet
        FOREIGN KEY (pet_id) REFERENCES pets(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_vaccinations_pet_id ON pet_vaccinations (pet_id);
CREATE INDEX IF NOT EXISTS idx_vaccinations_valid_until ON pet_vaccinations (valid_until);

------------------------------------------------------------
-- Table pet_documents : documents (passeport, certificat...) (metadata seulement)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS pet_documents (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    pet_id          uuid NOT NULL,

    doc_type        varchar(50) NOT NULL, -- passeport/certificat/assurance/autre
    title           text NOT NULL,
    media_id        uuid,                 -- MediaService
    url             text,
    issued_on       date,
    expires_on      date,

    created_at      timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_documents_pet
        FOREIGN KEY (pet_id) REFERENCES pets(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_documents_pet_id ON pet_documents (pet_id);

------------------------------------------------------------
-- Outbox (events PetCreated, PetUpdated, PetDeleted, VaccinationAdded, etc.)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS outbox_messages (
    id             bigserial PRIMARY KEY,
    message_id     uuid NOT NULL UNIQUE,
    aggregate_type varchar(100),
    aggregate_id   uuid,
    type           varchar(200) NOT NULL, -- ex: Pet.PetCreated
    payload        jsonb NOT NULL,
    occurred_on    timestamptz NOT NULL DEFAULT now(),
    processed_on   timestamptz,
    status         varchar(20) NOT NULL DEFAULT 'pending', -- pending/processed/failed
    error          text
);

CREATE INDEX IF NOT EXISTS idx_outbox_status ON outbox_messages (status);
CREATE INDEX IF NOT EXISTS idx_outbox_occurred_on ON outbox_messages (occurred_on);
