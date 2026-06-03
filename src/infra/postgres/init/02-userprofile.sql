SELECT 'CREATE DATABASE userprofile_db'
WHERE NOT EXISTS (
    SELECT FROM pg_database WHERE datname = 'userprofile_db'
)\gexec

\connect userprofile_db

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS user_profiles (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL UNIQUE,
    username text NULL UNIQUE,
    display_name text NULL,
    bio text NULL,
    avatar_url text NULL,
    banner_url text NULL,
    birthdate date NULL,
    city text NULL,
    country text NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS user_profile_media (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_id uuid NOT NULL REFERENCES user_profiles(id) ON DELETE CASCADE,
    media_id uuid NOT NULL,
    media_url text NULL,
    usage_type text NOT NULL DEFAULT 'gallery',
    display_order integer NOT NULL DEFAULT 0,
    caption text NULL,
    is_primary boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (profile_id, media_id, usage_type),
    CONSTRAINT ck_user_profile_media_usage_type
        CHECK (usage_type IN ('avatar', 'banner', 'gallery'))
);

CREATE INDEX IF NOT EXISTS ix_user_profiles_user_id ON user_profiles (user_id);
CREATE INDEX IF NOT EXISTS ix_user_profiles_username ON user_profiles (username);
CREATE INDEX IF NOT EXISTS ix_user_profile_media_profile_usage
    ON user_profile_media (profile_id, usage_type, display_order, created_at);
