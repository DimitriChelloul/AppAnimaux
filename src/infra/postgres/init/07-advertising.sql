SELECT 'CREATE DATABASE advertising_db'
WHERE NOT EXISTS (
    SELECT FROM pg_database WHERE datname = 'advertising_db'
)\gexec

\connect advertising_db

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS ad_campaigns (
    id uuid PRIMARY KEY,
    advertiser_user_id uuid NOT NULL,
    name text NOT NULL,
    objective text NOT NULL,
    daily_budget numeric(10, 2) NOT NULL,
    total_budget numeric(10, 2) NOT NULL,
    currency text NOT NULL DEFAULT 'EUR',
    status text NOT NULL DEFAULT 'draft',
    starts_at timestamptz NOT NULL,
    ends_at timestamptz NULL,
    frequency_cap_per_user_daily integer NULL DEFAULT 5,
    cooldown_minutes integer NULL DEFAULT 30,
    impressions_count bigint NOT NULL DEFAULT 0,
    clicks_count bigint NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_ad_campaigns_status
        CHECK (status IN ('draft', 'active', 'paused', 'archived')),
    CONSTRAINT ck_ad_campaigns_budget
        CHECK (daily_budget > 0 AND total_budget > 0 AND total_budget >= daily_budget),
    CONSTRAINT ck_ad_campaigns_dates
        CHECK (ends_at IS NULL OR ends_at >= starts_at),
    CONSTRAINT ck_ad_campaigns_frequency
        CHECK (
            (frequency_cap_per_user_daily IS NULL OR frequency_cap_per_user_daily BETWEEN 1 AND 100)
            AND (cooldown_minutes IS NULL OR cooldown_minutes BETWEEN 1 AND 1440)
        )
);

ALTER TABLE ad_campaigns
    ADD COLUMN IF NOT EXISTS frequency_cap_per_user_daily integer NULL DEFAULT 5;

ALTER TABLE ad_campaigns
    ADD COLUMN IF NOT EXISTS cooldown_minutes integer NULL DEFAULT 30;

CREATE TABLE IF NOT EXISTS ad_creatives (
    id uuid PRIMARY KEY,
    campaign_id uuid NOT NULL REFERENCES ad_campaigns(id) ON DELETE CASCADE,
    title text NOT NULL,
    body text NULL,
    media_url text NULL,
    landing_url text NOT NULL,
    placement text NOT NULL,
    weight integer NOT NULL DEFAULT 1,
    status text NOT NULL DEFAULT 'active',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_ad_creatives_status
        CHECK (status IN ('active', 'paused', 'archived')),
    CONSTRAINT ck_ad_creatives_weight
        CHECK (weight > 0)
);

CREATE TABLE IF NOT EXISTS ad_interactions (
    id uuid PRIMARY KEY,
    campaign_id uuid NOT NULL REFERENCES ad_campaigns(id) ON DELETE CASCADE,
    creative_id uuid NOT NULL REFERENCES ad_creatives(id) ON DELETE CASCADE,
    viewer_user_id uuid NULL,
    viewer_key text NULL,
    placement text NOT NULL,
    interaction_type text NOT NULL,
    landing_url text NULL,
    tracked_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_ad_interactions_type
        CHECK (interaction_type IN ('impression', 'click'))
);

ALTER TABLE ad_interactions
    ADD COLUMN IF NOT EXISTS viewer_key text NULL;

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

CREATE INDEX IF NOT EXISTS ix_ad_campaigns_advertiser_created
    ON ad_campaigns (advertiser_user_id, created_at DESC);

CREATE INDEX IF NOT EXISTS ix_ad_campaigns_status_schedule
    ON ad_campaigns (status, starts_at, ends_at);

CREATE INDEX IF NOT EXISTS ix_ad_creatives_placement_active
    ON ad_creatives (placement, status, weight);

CREATE INDEX IF NOT EXISTS ix_ad_interactions_campaign_type_tracked
    ON ad_interactions (campaign_id, interaction_type, tracked_at DESC);

CREATE INDEX IF NOT EXISTS ix_ad_interactions_viewer_campaign_impressions
    ON ad_interactions (viewer_user_id, campaign_id, tracked_at DESC)
    WHERE interaction_type = 'impression' AND viewer_user_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_ad_interactions_viewer_key_campaign_impressions
    ON ad_interactions (viewer_key, campaign_id, tracked_at DESC)
    WHERE interaction_type = 'impression' AND viewer_key IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_outbox_messages_status_occurred_on
    ON outbox_messages (status, occurred_on);
