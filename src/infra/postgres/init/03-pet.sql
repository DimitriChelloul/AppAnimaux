SELECT 'CREATE DATABASE pet_db'
WHERE NOT EXISTS (
    SELECT FROM pg_database WHERE datname = 'pet_db'
)\gexec

\connect pet_db

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS pets (
    id uuid PRIMARY KEY,
    owner_user_id uuid NOT NULL,
    name text NOT NULL,
    species text NOT NULL,
    breed text NULL,
    sex text NULL,
    birthdate date NULL,
    weight_kg numeric(7, 2) NULL,
    color text NULL,
    microchip_id text NULL,
    tattoo_id text NULL,
    is_neutered boolean NOT NULL DEFAULT false,
    allergies text NULL,
    notes text NULL,
    main_photo_media_id uuid NULL,
    main_photo_url text NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_pets_weight_positive CHECK (weight_kg IS NULL OR weight_kg > 0),
    CONSTRAINT ck_pets_sex CHECK (sex IS NULL OR sex IN ('male', 'female', 'unknown'))
);

CREATE TABLE IF NOT EXISTS pet_photos (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    pet_id uuid NOT NULL REFERENCES pets(id) ON DELETE CASCADE,
    media_id uuid NOT NULL,
    media_url text NULL,
    display_order integer NOT NULL DEFAULT 0,
    caption text NULL,
    is_primary boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (pet_id, media_id)
);

CREATE INDEX IF NOT EXISTS ix_pets_owner_created_at ON pets (owner_user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_pets_species ON pets (species);
CREATE INDEX IF NOT EXISTS ix_pet_photos_pet_display ON pet_photos (pet_id, display_order, created_at);
