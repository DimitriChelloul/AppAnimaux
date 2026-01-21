-- 01-reporting-schema.sql
-- Schéma du ReportingService (reporting_db)
-- Event store léger + métriques agrégées

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

------------------------------------------------------------
-- event_logs : stockage des événements consommés depuis RabbitMQ
-- Sert de base pour les agrégations et le debug (observabilité fonctionnelle).
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS event_logs (
    id              bigserial PRIMARY KEY,

    event_id        uuid NOT NULL UNIQUE,     -- id global event (correlation)
    event_type      varchar(200) NOT NULL,    -- ex: HelpRequest.Created, Messaging.MessageSent
    source_service  varchar(50) NOT NULL,     -- HelpRequestService, ForumService, etc.

    aggregate_type  varchar(100),             -- HelpRequest, Message, Topic...
    aggregate_id    uuid,

    actor_user_id   uuid,                     -- l'utilisateur à l'origine si connu
    occurred_on     timestamptz NOT NULL,     -- date event côté source
    received_on     timestamptz NOT NULL DEFAULT now(), -- date réception reporting

    payload         jsonb,                    -- payload brut (attention RGPD: éviter données sensibles)
    correlation_id  uuid,
    causation_id    uuid,

    meta            jsonb                     -- ex: ip, userAgent, versionApp...
);

CREATE INDEX IF NOT EXISTS idx_event_logs_type_time
ON event_logs (event_type, occurred_on DESC);

CREATE INDEX IF NOT EXISTS idx_event_logs_source_time
ON event_logs (source_service, occurred_on DESC);

CREATE INDEX IF NOT EXISTS idx_event_logs_actor_time
ON event_logs (actor_user_id, occurred_on DESC);

------------------------------------------------------------
-- daily_metrics : KPIs agrégés par jour
-- Exemple: nouveaux users, demandes créées, messages envoyés, etc.
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS daily_metrics (
    day             date NOT NULL,
    metric_key      varchar(100) NOT NULL, -- ex: "users.new", "helprequests.created"
    metric_value    bigint NOT NULL DEFAULT 0,
    dimensions      jsonb,                 -- ex: { "country":"FR", "platform":"android" }
    updated_at      timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (day, metric_key, dimensions)
);

-- (Optional) index GIN si tu filtres beaucoup par dimensions
-- CREATE INDEX IF NOT EXISTS idx_daily_metrics_dimensions ON daily_metrics USING gin (dimensions);

------------------------------------------------------------
-- user_metrics : métriques par user (optionnel)
-- Utile pour score confiance, activité, etc.
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS user_metrics (
    user_id         uuid NOT NULL,
    metric_key      varchar(100) NOT NULL, -- ex: "messages.sent", "helps.completed"
    metric_value    bigint NOT NULL DEFAULT 0,
    updated_at      timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, metric_key)
);

------------------------------------------------------------
-- materialized_views_status : suivi des jobs d'agrégation
-- Exemple: job qui calcule daily_metrics toutes les heures / la nuit.
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS aggregation_jobs (
    id              bigserial PRIMARY KEY,
    job_name        varchar(100) NOT NULL, -- ex: "daily_aggregation"
    status          varchar(20) NOT NULL DEFAULT 'success', -- running/success/failed
    started_at      timestamptz,
    finished_at     timestamptz,
    last_processed_event_id uuid,          -- checkpoint pour reprise
    error           text,
    created_at      timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_aggregation_jobs_name_time
ON aggregation_jobs (job_name, created_at DESC);

------------------------------------------------------------
-- outbox_messages : si ReportingService doit republier des events (optionnel)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS outbox_messages (
    id             bigserial PRIMARY KEY,
    message_id     uuid NOT NULL UNIQUE,
    aggregate_type varchar(100),
    aggregate_id   uuid,
    type           varchar(200) NOT NULL, -- ex: Reporting.DailyMetricsUpdated
    payload        jsonb NOT NULL,
    occurred_on    timestamptz NOT NULL DEFAULT now(),
    processed_on   timestamptz,
    status         varchar(20) NOT NULL DEFAULT 'pending', -- pending/processed/failed
    error          text
);

CREATE INDEX IF NOT EXISTS idx_outbox_status ON outbox_messages (status);
CREATE INDEX IF NOT EXISTS idx_outbox_occurred_on ON outbox_messages (occurred_on);
