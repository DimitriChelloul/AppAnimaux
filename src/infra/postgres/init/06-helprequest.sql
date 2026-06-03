SELECT 'CREATE DATABASE helprequest_db'
WHERE NOT EXISTS (
    SELECT FROM pg_database WHERE datname = 'helprequest_db'
)\gexec

\connect helprequest_db

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS help_requests (
    id uuid PRIMARY KEY,
    requester_user_id uuid NOT NULL,
    pet_id uuid NULL,
    title text NOT NULL,
    description text NULL,
    help_type text NOT NULL,
    status text NOT NULL DEFAULT 'draft',
    city text NULL,
    postal_code text NULL,
    latitude double precision NULL,
    longitude double precision NULL,
    start_at timestamptz NULL,
    end_at timestamptz NULL,
    is_paid boolean NOT NULL DEFAULT false,
    budget_amount numeric(10, 2) NULL,
    currency text NOT NULL DEFAULT 'EUR',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    closed_at timestamptz NULL,
    CONSTRAINT ck_help_requests_status
        CHECK (status IN ('draft', 'published', 'accepted', 'in_progress', 'completed', 'cancelled')),
    CONSTRAINT ck_help_requests_budget
        CHECK (budget_amount IS NULL OR budget_amount >= 0),
    CONSTRAINT ck_help_requests_dates
        CHECK (end_at IS NULL OR start_at IS NULL OR end_at >= start_at)
);

CREATE TABLE IF NOT EXISTS help_offers (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    help_request_id uuid NOT NULL REFERENCES help_requests(id) ON DELETE CASCADE,
    helper_user_id uuid NOT NULL,
    message text NULL,
    proposed_amount numeric(10, 2) NULL,
    currency text NOT NULL DEFAULT 'EUR',
    status text NOT NULL DEFAULT 'pending',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (help_request_id, helper_user_id),
    CONSTRAINT ck_help_offers_status
        CHECK (status IN ('pending', 'accepted', 'rejected', 'cancelled')),
    CONSTRAINT ck_help_offers_amount
        CHECK (proposed_amount IS NULL OR proposed_amount >= 0)
);

CREATE TABLE IF NOT EXISTS help_matches (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    help_request_id uuid NOT NULL REFERENCES help_requests(id) ON DELETE CASCADE,
    accepted_offer_id uuid NOT NULL REFERENCES help_offers(id) ON DELETE RESTRICT,
    requester_user_id uuid NOT NULL,
    helper_user_id uuid NOT NULL,
    status text NOT NULL DEFAULT 'active',
    started_at timestamptz NULL,
    completed_at timestamptz NULL,
    cancelled_at timestamptz NULL,
    cancel_reason text NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (help_request_id),
    CONSTRAINT ck_help_matches_status
        CHECK (status IN ('active', 'completed', 'cancelled'))
);

CREATE TABLE IF NOT EXISTS outbox_messages (
    id bigserial PRIMARY KEY,
    message_id uuid NOT NULL UNIQUE,
    aggregate_type text NULL,
    aggregate_id uuid NULL,
    type text NOT NULL,
    payload jsonb NOT NULL,
    occurred_on timestamptz NOT NULL DEFAULT now(),
    status text NOT NULL DEFAULT 'pending',
    processed_on timestamptz NULL,
    error text NULL
);

CREATE INDEX IF NOT EXISTS ix_help_requests_requester_created
    ON help_requests (requester_user_id, created_at DESC);

CREATE INDEX IF NOT EXISTS ix_help_requests_search
    ON help_requests (status, help_type, start_at, created_at DESC);

CREATE INDEX IF NOT EXISTS ix_help_offers_request
    ON help_offers (help_request_id, created_at);

CREATE INDEX IF NOT EXISTS ix_help_offers_helper
    ON help_offers (helper_user_id, created_at DESC);

CREATE INDEX IF NOT EXISTS ix_help_matches_users
    ON help_matches (requester_user_id, helper_user_id, created_at DESC);

CREATE INDEX IF NOT EXISTS ix_outbox_messages_status_occurred_on
    ON outbox_messages (status, occurred_on);
