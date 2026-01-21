-- 01-helprequest-schema.sql
-- Schéma du HelpRequestService (helprequest_db)

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

------------------------------------------------------------
-- help_requests : demande d'aide (créée par un demandeur)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS help_requests (
    id                uuid PRIMARY KEY DEFAULT uuid_generate_v4(),

    requester_user_id uuid NOT NULL,  -- IdentityService (pas de FK inter-db)
    pet_id            uuid,           -- PetService (pas de FK inter-db)

    title             text NOT NULL,
    description       text,

    help_type         varchar(50) NOT NULL,  -- garde/promenade/visite/covoiturage/etc.
    status            varchar(20) NOT NULL DEFAULT 'open', -- open/matched/in_progress/completed/cancelled

    -- localisation (simplifiée, la source de verite peut etre LocationService)
    city              text,
    postal_code       varchar(20),
    latitude          double precision,
    longitude         double precision,

    -- fenetre temporelle de la demande
    start_at          timestamptz,
    end_at            timestamptz,

    -- options business
    is_paid           boolean NOT NULL DEFAULT false,
    budget_amount     numeric(10,2),
    currency          varchar(3) NOT NULL DEFAULT 'EUR',

    created_at        timestamptz NOT NULL DEFAULT now(),
    updated_at        timestamptz NOT NULL DEFAULT now(),
    closed_at         timestamptz
);

CREATE INDEX IF NOT EXISTS idx_help_requests_requester ON help_requests (requester_user_id);
CREATE INDEX IF NOT EXISTS idx_help_requests_status ON help_requests (status);
CREATE INDEX IF NOT EXISTS idx_help_requests_help_type ON help_requests (help_type);
CREATE INDEX IF NOT EXISTS idx_help_requests_start_end ON help_requests (start_at, end_at);

------------------------------------------------------------
-- help_offers : offre d'aide faite par un utilisateur (helper)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS help_offers (
    id               uuid PRIMARY KEY DEFAULT uuid_generate_v4(),

    help_request_id  uuid NOT NULL,
    helper_user_id   uuid NOT NULL, -- IdentityService

    message          text,
    proposed_amount  numeric(10,2),
    currency         varchar(3) NOT NULL DEFAULT 'EUR',

    status           varchar(20) NOT NULL DEFAULT 'pending', -- pending/accepted/rejected/cancelled

    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_help_offers_request
        FOREIGN KEY (help_request_id) REFERENCES help_requests(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_help_offers_request ON help_offers (help_request_id);
CREATE INDEX IF NOT EXISTS idx_help_offers_helper ON help_offers (helper_user_id);
CREATE INDEX IF NOT EXISTS idx_help_offers_status ON help_offers (status);

-- Eviter qu'un meme helper fasse 50 offres sur la meme demande
CREATE UNIQUE INDEX IF NOT EXISTS uq_help_offers_request_helper
ON help_offers (help_request_id, helper_user_id);

------------------------------------------------------------
-- help_matches : match final (quand une offre est acceptée)
-- 1 request -> 0..1 match actif (en général)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS help_matches (
    id               uuid PRIMARY KEY DEFAULT uuid_generate_v4(),

    help_request_id  uuid NOT NULL UNIQUE,
    accepted_offer_id uuid NOT NULL UNIQUE,

    requester_user_id uuid NOT NULL,
    helper_user_id   uuid NOT NULL,

    status           varchar(20) NOT NULL DEFAULT 'active', -- active/completed/cancelled/disputed

    started_at       timestamptz,
    completed_at     timestamptz,
    cancelled_at     timestamptz,
    cancel_reason    text,

    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_help_matches_request
        FOREIGN KEY (help_request_id) REFERENCES help_requests(id) ON DELETE CASCADE,

    CONSTRAINT fk_help_matches_offer
        FOREIGN KEY (accepted_offer_id) REFERENCES help_offers(id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS idx_help_matches_requester ON help_matches (requester_user_id);
CREATE INDEX IF NOT EXISTS idx_help_matches_helper ON help_matches (helper_user_id);
CREATE INDEX IF NOT EXISTS idx_help_matches_status ON help_matches (status);

------------------------------------------------------------
-- help_request_events : historique de statuts / timeline (audit metier)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS help_request_events (
    id              bigserial PRIMARY KEY,
    help_request_id uuid NOT NULL,
    event_type      varchar(50) NOT NULL, -- created/offer_received/offer_accepted/status_changed/cancelled/etc.
    data            jsonb,
    created_at      timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_help_events_request
        FOREIGN KEY (help_request_id) REFERENCES help_requests(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_help_events_request ON help_request_events (help_request_id);
CREATE INDEX IF NOT EXISTS idx_help_events_created_at ON help_request_events (created_at);

------------------------------------------------------------
-- outbox_messages : pattern outbox (events vers RabbitMQ)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS outbox_messages (
    id             bigserial PRIMARY KEY,
    message_id     uuid NOT NULL UNIQUE,
    aggregate_type varchar(100),
    aggregate_id   uuid,
    type           varchar(200) NOT NULL, -- ex: HelpRequest.Created / HelpOffer.Accepted
    payload        jsonb NOT NULL,
    occurred_on    timestamptz NOT NULL DEFAULT now(),
    processed_on   timestamptz,
    status         varchar(20) NOT NULL DEFAULT 'pending', -- pending/processed/failed
    error          text
);

CREATE INDEX IF NOT EXISTS idx_outbox_status ON outbox_messages (status);
CREATE INDEX IF NOT EXISTS idx_outbox_occurred_on ON outbox_messages (occurred_on);
