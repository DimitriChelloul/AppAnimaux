SELECT 'CREATE DATABASE media_db'
WHERE NOT EXISTS (
    SELECT FROM pg_database WHERE datname = 'media_db'
)\gexec

\connect media_db

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS media_files (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_user_id uuid NOT NULL,
    file_name text NOT NULL,
    content_type text NOT NULL,
    size_bytes bigint NOT NULL,
    checksum_sha256 text NULL,
    width integer NULL,
    height integer NULL,
    duration_seconds integer NULL,
    storage_provider text NOT NULL DEFAULT 'local',
    storage_bucket text NULL,
    storage_key text NOT NULL,
    public_url text NULL,
    is_public boolean NOT NULL DEFAULT false,
    status text NOT NULL DEFAULT 'active',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz NULL,
    CONSTRAINT ck_media_files_size CHECK (size_bytes > 0),
    CONSTRAINT ck_media_files_status CHECK (status IN ('active', 'pending', 'deleted'))
);

CREATE TABLE IF NOT EXISTS media_usages (
    id bigserial PRIMARY KEY,
    media_id uuid NOT NULL REFERENCES media_files(id) ON DELETE CASCADE,
    service_name text NOT NULL,
    entity_type text NOT NULL,
    entity_id uuid NOT NULL,
    usage_type text NOT NULL DEFAULT 'attachment',
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_media_usages_target UNIQUE (media_id, service_name, entity_type, entity_id, usage_type)
);

CREATE TABLE IF NOT EXISTS frontend_assets (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    media_id uuid NOT NULL REFERENCES media_files(id) ON DELETE CASCADE,
    asset_key text NOT NULL,
    asset_type text NOT NULL,
    platform text NOT NULL DEFAULT 'all',
    theme text NOT NULL DEFAULT 'default',
    locale text NULL,
    display_name text NULL,
    description text NULL,
    is_active boolean NOT NULL DEFAULT true,
    sort_order integer NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_frontend_assets_lookup
    ON frontend_assets (asset_key, platform, theme, COALESCE(locale, ''));

CREATE INDEX IF NOT EXISTS ix_media_files_owner_created
    ON media_files (owner_user_id, created_at DESC);

CREATE INDEX IF NOT EXISTS ix_media_usages_target
    ON media_usages (service_name, entity_type, entity_id);

CREATE INDEX IF NOT EXISTS ix_frontend_assets_active_lookup
    ON frontend_assets (asset_key, platform, theme, is_active);
