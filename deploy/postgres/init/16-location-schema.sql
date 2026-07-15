-- 16-location-schema.sql
-- Schéma de la base location_db (service LocationService)
connect location_db

-- Si tu es dans psql :
-- \connect location_db;

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "citext";

------------------------------------------------------------
-- Table user_locations : localisation principale des users
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS user_locations (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id         uuid        NOT NULL, -- référence logique vers Identity/UserProfile
    latitude        numeric(9,6)  NOT NULL,
    longitude       numeric(9,6)  NOT NULL,
    city            varchar(150),
    postal_code     varchar(20),
    country         varchar(100),
    is_active       boolean     NOT NULL DEFAULT true,
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_user_locations_user_id
    ON user_locations (user_id);

CREATE INDEX IF NOT EXISTS idx_user_locations_coords
    ON user_locations (latitude, longitude);

------------------------------------------------------------
-- Table location_preferences : préférences de recherche
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS location_preferences (
    id                  uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id             uuid NOT NULL,
    search_radius_km    int  NOT NULL DEFAULT 10,
    allow_remote        boolean NOT NULL DEFAULT false,
    notify_on_match     boolean NOT NULL DEFAULT true,
    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_location_preferences_user
    ON location_preferences (user_id);

------------------------------------------------------------
-- Table geo_cache : cache de géocodage (API externe)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS geo_cache (
    id              bigserial PRIMARY KEY,
    query           text        NOT NULL,
    latitude        numeric(9,6) NOT NULL,
    longitude       numeric(9,6) NOT NULL,
    provider        varchar(50) NOT NULL, -- google, mapbox, nominatim
    created_at      timestamptz NOT NULL DEFAULT now(),
    expires_at      timestamptz
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_geo_cache_query_provider
    ON geo_cache (query, provider);

------------------------------------------------------------
-- Table distance_calculations : traces / debug (optionnel)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS distance_calculations (
    id              bigserial PRIMARY KEY,
    origin_lat      numeric(9,6) NOT NULL,
    origin_lng      numeric(9,6) NOT NULL,
    target_lat      numeric(9,6) NOT NULL,
    target_lng      numeric(9,6) NOT NULL,
    distance_km     numeric(10,3) NOT NULL,
    calculated_at   timestamptz  NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- Table outbox_messages : pattern outbox (event-driven)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS outbox_messages (
    id             bigserial PRIMARY KEY,
    message_id     uuid NOT NULL UNIQUE,
    aggregate_type varchar(100),       -- Location
    aggregate_id   uuid,
    type           varchar(200) NOT NULL, -- ex : Location.UserMoved
    payload        jsonb        NOT NULL,
    occurred_on    timestamptz  NOT NULL DEFAULT now(),
    processed_on   timestamptz,
    status         varchar(20)  NOT NULL DEFAULT 'pending',
    error          text
);

CREATE INDEX IF NOT EXISTS idx_location_outbox_status
    ON outbox_messages (status);

CREATE INDEX IF NOT EXISTS idx_location_outbox_occurred_on
    ON outbox_messages (occurred_on);
