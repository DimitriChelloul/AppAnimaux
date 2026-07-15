-- 01-userprofile-schema.sql
-- Schéma du UserProfileService (userprofile_db)
connect userprofile_db

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "citext";

------------------------------------------------------------
-- Table user_profiles : profil public de l'utilisateur
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS user_profiles (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),

    user_id         uuid NOT NULL UNIQUE, 
    -- lien vers identity_db mais SANS FK (car microservices separent les BDD)

    username        citext UNIQUE, 
    -- pseudo public (case-insensitive)

    display_name    text,
    bio             text,
    avatar_url      text,
    banner_url      text,

    birthdate       date,
    city            text,
    country         text,

    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_user_profiles_user_id ON user_profiles (user_id);

------------------------------------------------------------
-- Table user_profile_media : galerie de photos du profil
-- Les fichiers restent dans MediaService ; cette table garde l'ordre
-- et le role metier cote UserProfileService.
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS user_profile_media (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    profile_id      uuid NOT NULL,
    media_id        uuid NOT NULL,
    media_url       text,
    usage_type      varchar(30) NOT NULL DEFAULT 'gallery', -- avatar/banner/gallery
    display_order   int NOT NULL DEFAULT 0,
    caption         text,
    is_primary      boolean NOT NULL DEFAULT false,
    created_at      timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_user_profile_media_profile
        FOREIGN KEY (profile_id) REFERENCES user_profiles(id) ON DELETE CASCADE,
    CONSTRAINT ck_user_profile_media_usage
        CHECK (usage_type IN ('avatar', 'banner', 'gallery'))
);

CREATE INDEX IF NOT EXISTS idx_user_profile_media_profile_id ON user_profile_media (profile_id);
CREATE UNIQUE INDEX IF NOT EXISTS uq_user_profile_media_media_id ON user_profile_media (profile_id, media_id, usage_type);
CREATE INDEX IF NOT EXISTS idx_user_profile_media_order ON user_profile_media (profile_id, display_order);

------------------------------------------------------------
-- Table user_preferences : reglages utilisateur
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS user_preferences (
    id                  uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id             uuid NOT NULL,

    notifications_email boolean NOT NULL DEFAULT true,
    notifications_push  boolean NOT NULL DEFAULT true,
    theme               varchar(20) DEFAULT 'light',
    language            varchar(10) DEFAULT 'fr',

    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_user_preferences_user_id ON user_preferences (user_id);

------------------------------------------------------------
-- Table outbox_messages : pattern outbox
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS outbox_messages (
    id             bigserial PRIMARY KEY,
    message_id     uuid NOT NULL UNIQUE,
    aggregate_type varchar(100),
    aggregate_id   uuid,
    type           varchar(200) NOT NULL,     -- ex : UserProfile.Updated
    payload        jsonb NOT NULL,
    occurred_on    timestamptz NOT NULL DEFAULT now(),
    processed_on   timestamptz,
    status         varchar(20) NOT NULL DEFAULT 'pending',  -- pending / processed / failed
    error          text
);

CREATE INDEX IF NOT EXISTS idx_outbox_status ON outbox_messages (status);
CREATE INDEX IF NOT EXISTS idx_outbox_created ON outbox_messages (occurred_on);

CREATE TABLE IF NOT EXISTS inbox_messages (
    message_id  uuid PRIMARY KEY,
    event_type  varchar(200) NOT NULL,
    processed_on timestamptz NOT NULL DEFAULT now()
);
