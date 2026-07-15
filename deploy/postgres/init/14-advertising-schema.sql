-- 01-advertising-schema.sql
-- Schéma du AdvertisingService (advertising_db)
connect advertising_db
-- Objectif: campagnes, impressions, clics, budget, ciblage

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

------------------------------------------------------------
-- ad_campaigns : campagnes pub
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ad_campaigns (
    id               uuid PRIMARY KEY DEFAULT uuid_generate_v4(),

    advertiser_user_id uuid NOT NULL, -- IdentityService (annonceur)
    name             text NOT NULL,
    status           varchar(20) NOT NULL DEFAULT 'draft', -- draft/active/paused/ended

    objective        varchar(30) NOT NULL DEFAULT 'clicks', -- clicks/impressions/leads
    daily_budget     numeric(10,2),
    total_budget     numeric(10,2),
    currency         varchar(3) NOT NULL DEFAULT 'EUR',

    start_at         timestamptz,
    end_at           timestamptz,

    targeting        jsonb, -- ex: {"city":"Paris","radiusKm":20,"species":["dog","cat"]}
    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_campaigns_advertiser ON ad_campaigns (advertiser_user_id);
CREATE INDEX IF NOT EXISTS idx_campaigns_status ON ad_campaigns (status);

------------------------------------------------------------
-- ad_creatives : visuels/textes de la pub
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ad_creatives (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    campaign_id     uuid NOT NULL,

    title           text,
    body            text,
    call_to_action  varchar(50),
    landing_url     text NOT NULL,

    media_id        uuid, -- MediaService (pas de FK inter-db)
    media_url       text,

    status          varchar(20) NOT NULL DEFAULT 'active', -- active/disabled

    created_at      timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_creatives_campaign
        FOREIGN KEY (campaign_id) REFERENCES ad_campaigns(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_creatives_campaign ON ad_creatives (campaign_id);

------------------------------------------------------------
-- ad_impressions : journal des impressions (peut grossir)
-- En prod on mettrait plutôt un système d'analytics + agrégation.
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ad_impressions (
    id              bigserial PRIMARY KEY,
    campaign_id     uuid NOT NULL,
    creative_id     uuid NOT NULL,

    viewer_user_id  uuid, -- utilisateur qui voit la pub (si connecté)
    placement       varchar(50) NOT NULL, -- home/feed/search/details
    occurred_on     timestamptz NOT NULL DEFAULT now(),
    meta            jsonb,

    CONSTRAINT fk_impr_campaign
        FOREIGN KEY (campaign_id) REFERENCES ad_campaigns(id) ON DELETE CASCADE,

    CONSTRAINT fk_impr_creative
        FOREIGN KEY (creative_id) REFERENCES ad_creatives(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_impr_time ON ad_impressions (occurred_on DESC);
CREATE INDEX IF NOT EXISTS idx_impr_campaign ON ad_impressions (campaign_id);

------------------------------------------------------------
-- ad_clicks : journal des clics
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ad_clicks (
    id              bigserial PRIMARY KEY,
    campaign_id     uuid NOT NULL,
    creative_id     uuid NOT NULL,

    viewer_user_id  uuid,
    occurred_on     timestamptz NOT NULL DEFAULT now(),
    meta            jsonb,

    CONSTRAINT fk_click_campaign
        FOREIGN KEY (campaign_id) REFERENCES ad_campaigns(id) ON DELETE CASCADE,

    CONSTRAINT fk_click_creative
        FOREIGN KEY (creative_id) REFERENCES ad_creatives(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_click_time ON ad_clicks (occurred_on DESC);
CREATE INDEX IF NOT EXISTS idx_click_campaign ON ad_clicks (campaign_id);

------------------------------------------------------------
-- outbox_messages
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS outbox_messages (
    id             bigserial PRIMARY KEY,
    message_id     uuid NOT NULL UNIQUE,
    aggregate_type varchar(100),
    aggregate_id   uuid,
    type           varchar(200) NOT NULL, -- Ads.CampaignActivated / Ads.ClickTracked ...
    payload        jsonb NOT NULL,
    occurred_on    timestamptz NOT NULL DEFAULT now(),
    processed_on   timestamptz,
    status         varchar(20) NOT NULL DEFAULT 'pending',
    error          text,
    attempts       integer NOT NULL DEFAULT 0,
    next_attempt_on timestamptz
);

CREATE INDEX IF NOT EXISTS idx_outbox_status ON outbox_messages (status);
CREATE INDEX IF NOT EXISTS idx_outbox_occurred_on ON outbox_messages (occurred_on);
