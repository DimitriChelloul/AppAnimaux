-- 17-alert-schema.sql
-- Schéma de la base alert_db (service AlertService)
connect alert_db

-- Si tu es dans psql :
-- \connect alert_db;

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "citext";

------------------------------------------------------------
-- Types (simples, lisibles, compatibles app)
------------------------------------------------------------
-- type d’alerte : perdu / trouvé / danger / info locale
CREATE TABLE IF NOT EXISTS alert_types (
    id          smallserial PRIMARY KEY,
    name        varchar(50) NOT NULL UNIQUE,     -- lost / found / danger / local_info
    description text,
    created_at  timestamptz NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- Table alerts : alerte principale (créée par un user)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS alerts (
    id                  uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id             uuid NOT NULL, -- référence logique vers Identity/UserProfile
    pet_id              uuid,          -- référence logique vers PetService (si alerte liée à un animal)
    alert_type_id       smallint NOT NULL,
    title               varchar(200) NOT NULL,
    description         text,
    status              varchar(20) NOT NULL DEFAULT 'active', -- active / resolved / cancelled / archived
    severity            varchar(20) NOT NULL DEFAULT 'normal', -- low / normal / high

    -- Position (sans PostGIS)
    latitude            numeric(9,6),
    longitude           numeric(9,6),
    city                varchar(150),
    postal_code         varchar(20),
    country             varchar(100),

    -- Fenêtre de validité
    starts_at           timestamptz NOT NULL DEFAULT now(),
    ends_at             timestamptz,
    resolved_at         timestamptz,

    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_alerts_alert_type
        FOREIGN KEY (alert_type_id) REFERENCES alert_types(id)
);

CREATE INDEX IF NOT EXISTS idx_alerts_user_id
    ON alerts (user_id);

CREATE INDEX IF NOT EXISTS idx_alerts_pet_id
    ON alerts (pet_id);

CREATE INDEX IF NOT EXISTS idx_alerts_status
    ON alerts (status);

CREATE INDEX IF NOT EXISTS idx_alerts_type
    ON alerts (alert_type_id);

CREATE INDEX IF NOT EXISTS idx_alerts_coords
    ON alerts (latitude, longitude);

CREATE INDEX IF NOT EXISTS idx_alerts_created_at
    ON alerts (created_at);

------------------------------------------------------------
-- Table alert_media : médias liés à une alerte (photos/vidéos)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS alert_media (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    alert_id        uuid NOT NULL,
    media_id        uuid NOT NULL, -- référence logique vers MediaService
    media_type      varchar(30) NOT NULL DEFAULT 'image', -- image / video / other
    created_at      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_alert_media_alert
        FOREIGN KEY (alert_id) REFERENCES alerts(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_alert_media_alert_id
    ON alert_media (alert_id);

------------------------------------------------------------
-- Table alert_subscriptions : abonnements aux alertes locales
-- (pour recevoir des notifications selon zone + types)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS alert_subscriptions (
    id                  uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id             uuid NOT NULL,
    radius_km           int  NOT NULL DEFAULT 10,
    alert_type_id       smallint, -- NULL = tous types
    severity_min        varchar(20) NOT NULL DEFAULT 'low', -- low / normal / high
    is_enabled          boolean NOT NULL DEFAULT true,

    -- zone (sans PostGIS)
    center_latitude     numeric(9,6),
    center_longitude    numeric(9,6),
    city                varchar(150),
    postal_code         varchar(20),
    country             varchar(100),

    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_alert_subscriptions_type
        FOREIGN KEY (alert_type_id) REFERENCES alert_types(id)
);

CREATE INDEX IF NOT EXISTS idx_alert_subscriptions_user_id
    ON alert_subscriptions (user_id);

CREATE INDEX IF NOT EXISTS idx_alert_subscriptions_enabled
    ON alert_subscriptions (is_enabled);

CREATE INDEX IF NOT EXISTS idx_alert_subscriptions_center
    ON alert_subscriptions (center_latitude, center_longitude);

------------------------------------------------------------
-- Table alert_reports : signalements (abus, faux, spam)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS alert_reports (
    id              bigserial PRIMARY KEY,
    alert_id        uuid NOT NULL,
    reporter_user_id uuid NOT NULL,
    reason          varchar(100) NOT NULL, -- spam / fake / abuse / other
    details         text,
    created_at      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_alert_reports_alert
        FOREIGN KEY (alert_id) REFERENCES alerts(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_alert_reports_alert_id
    ON alert_reports (alert_id);

CREATE INDEX IF NOT EXISTS idx_alert_reports_created_at
    ON alert_reports (created_at);

------------------------------------------------------------
-- Table alert_audit : historique des changements de statut
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS alert_audit (
    id              bigserial PRIMARY KEY,
    alert_id        uuid NOT NULL,
    changed_by      uuid, -- user/admin (logique)
    old_status      varchar(20),
    new_status      varchar(20) NOT NULL,
    note            text,
    created_at      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_alert_audit_alert
        FOREIGN KEY (alert_id) REFERENCES alerts(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_alert_audit_alert_id
    ON alert_audit (alert_id);

CREATE INDEX IF NOT EXISTS idx_alert_audit_created_at
    ON alert_audit (created_at);

------------------------------------------------------------
-- Table outbox_messages : pattern outbox pour publier des events
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS outbox_messages (
    id             bigserial PRIMARY KEY,
    message_id     uuid NOT NULL UNIQUE,
    aggregate_type varchar(100),         -- Alert
    aggregate_id   uuid,
    type           varchar(200) NOT NULL, -- ex : Alert.Created / Alert.Resolved
    payload        jsonb        NOT NULL,
    occurred_on    timestamptz  NOT NULL DEFAULT now(),
    processed_on   timestamptz,
    status         varchar(20)  NOT NULL DEFAULT 'pending', -- pending / processed / failed
    error          text
);

CREATE INDEX IF NOT EXISTS idx_alert_outbox_status
    ON outbox_messages (status);

CREATE INDEX IF NOT EXISTS idx_alert_outbox_occurred_on
    ON outbox_messages (occurred_on);

CREATE TABLE IF NOT EXISTS inbox_messages (
    message_id  uuid PRIMARY KEY,
    event_type  varchar(200) NOT NULL,
    processed_on timestamptz NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- Données de base : types d’alertes (id stables)
------------------------------------------------------------
INSERT INTO alert_types (name, description)
VALUES
  ('lost', 'Alerte animal perdu'),
  ('found', 'Alerte animal trouvé'),
  ('danger', 'Alerte danger (agression, chien dangereux, etc.)'),
  ('local_info', 'Information locale (zone à éviter, rappel, etc.)')
ON CONFLICT (name) DO NOTHING;
