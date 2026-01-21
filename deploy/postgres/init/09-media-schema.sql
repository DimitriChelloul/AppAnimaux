-- 01-media-schema.sql
-- Schéma du MediaService (media_db)
-- Stocke la METADATA des fichiers. Le binaire peut être en disque/S3/minio.

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

------------------------------------------------------------
-- media_files : metadata d'un fichier (photo, video, doc)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS media_files (
    id                 uuid PRIMARY KEY DEFAULT uuid_generate_v4(),

    owner_user_id       uuid NOT NULL,  -- IdentityService (pas de FK inter-db)

    file_name           text NOT NULL,
    content_type        varchar(150) NOT NULL,   -- image/png, image/jpeg, video/mp4...
    size_bytes          bigint NOT NULL CHECK (size_bytes >= 0),

    checksum_sha256     varchar(64),            -- optionnel
    width               int,
    height              int,
    duration_seconds    int,                    -- pour video/audio

    storage_provider    varchar(30) NOT NULL DEFAULT 'local', -- local/s3/minio/azureblob...
    storage_bucket      text,
    storage_key         text NOT NULL,          -- chemin/clé dans le provider
    public_url          text,                   -- si exposé via CDN/proxy
    is_public           boolean NOT NULL DEFAULT false,

    status              varchar(20) NOT NULL DEFAULT 'active', -- active/processing/failed/deleted

    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now(),

    deleted_at          timestamptz,
    deleted_by_user_id  uuid,
    delete_reason       text
);

CREATE INDEX IF NOT EXISTS idx_media_owner_user_id ON media_files (owner_user_id);
CREATE INDEX IF NOT EXISTS idx_media_status ON media_files (status);
CREATE UNIQUE INDEX IF NOT EXISTS uq_media_storage ON media_files (storage_provider, storage_key);

------------------------------------------------------------
-- media_variants : miniatures / versions redimensionnées
-- ex: thumb, small, medium...
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS media_variants (
    id             uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    media_id        uuid NOT NULL,

    variant_name    varchar(50) NOT NULL,     -- thumb/small/medium/large
    content_type    varchar(150) NOT NULL,
    size_bytes      bigint NOT NULL CHECK (size_bytes >= 0),

    width           int,
    height          int,

    storage_provider varchar(30) NOT NULL DEFAULT 'local',
    storage_bucket   text,
    storage_key      text NOT NULL,
    public_url       text,

    created_at      timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_variants_media
        FOREIGN KEY (media_id) REFERENCES media_files(id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_media_variant
ON media_variants (media_id, variant_name);

------------------------------------------------------------
-- media_usages : références métier (qui utilise ce média ?)
-- Permet de retrouver et nettoyer les médias orphelins.
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS media_usages (
    id             bigserial PRIMARY KEY,
    media_id        uuid NOT NULL,

    service_name    varchar(50) NOT NULL,   -- PetService/ForumService/MessagingService/UserProfileService...
    entity_type     varchar(50) NOT NULL,   -- pet/topic/post/message/profile...
    entity_id       uuid NOT NULL,

    usage_type      varchar(30) NOT NULL DEFAULT 'attachment', -- avatar/cover/attachment/thumbnail...
    created_at      timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_usages_media
        FOREIGN KEY (media_id) REFERENCES media_files(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_media_usages_media_id ON media_usages (media_id);
CREATE INDEX IF NOT EXISTS idx_media_usages_entity ON media_usages (service_name, entity_type, entity_id);

-- Evite doublons exacts
CREATE UNIQUE INDEX IF NOT EXISTS uq_media_usages_unique
ON media_usages (media_id, service_name, entity_type, entity_id, usage_type);

------------------------------------------------------------
-- outbox_messages : events sortants vers RabbitMQ
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS outbox_messages (
    id             bigserial PRIMARY KEY,
    message_id     uuid NOT NULL UNIQUE,
    aggregate_type varchar(100),
    aggregate_id   uuid,
    type           varchar(200) NOT NULL, -- ex: Media.Uploaded / Media.Deleted / Media.VariantCreated
    payload        jsonb NOT NULL,
    occurred_on    timestamptz NOT NULL DEFAULT now(),
    processed_on   timestamptz,
    status         varchar(20) NOT NULL DEFAULT 'pending', -- pending/processed/failed
    error          text
);

CREATE INDEX IF NOT EXISTS idx_outbox_status ON outbox_messages (status);
CREATE INDEX IF NOT EXISTS idx_outbox_occurred_on ON outbox_messages (occurred_on);
