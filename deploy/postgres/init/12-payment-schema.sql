-- 01-payment-schema.sql
-- Schéma du PaymentService (payment_db)
-- Objectif: paiements, remboursements, webhooks, et lien vers abonnements/credits.

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "citext";

------------------------------------------------------------
-- payment_customers : mapping user -> customer provider (ex Stripe Customer)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS payment_customers (
    id                  uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id              uuid NOT NULL UNIQUE, -- IdentityService

    provider            varchar(30) NOT NULL DEFAULT 'stripe', -- stripe/paypal/...
    provider_customer_id text NOT NULL,                        -- ex: cus_...

    email               citext, -- optionnel (si tu veux debug)
    created_at          timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_payment_customers_provider
ON payment_customers (provider, provider_customer_id);

------------------------------------------------------------
-- payment_methods : moyens de paiement tokenisés (pas de PAN !)
-- On stocke uniquement des infos non sensibles (brand, last4).
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS payment_methods (
    id                    uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id                uuid NOT NULL,

    provider              varchar(30) NOT NULL DEFAULT 'stripe',
    provider_payment_method_id text NOT NULL,   -- ex: pm_...

    type                  varchar(20) NOT NULL DEFAULT 'card', -- card/sepa/paypal/...
    brand                 varchar(30),
    last4                 varchar(4),
    exp_month             int,
    exp_year              int,

    is_default             boolean NOT NULL DEFAULT false,
    is_active              boolean NOT NULL DEFAULT true,

    created_at             timestamptz NOT NULL DEFAULT now(),
    updated_at             timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_payment_methods_user ON payment_methods (user_id);

-- 1 méthode "default" active max par user
CREATE UNIQUE INDEX IF NOT EXISTS uq_payment_methods_default
ON payment_methods (user_id)
WHERE is_default = true AND is_active = true;

CREATE UNIQUE INDEX IF NOT EXISTS uq_payment_methods_provider_pm
ON payment_methods (provider, provider_payment_method_id);

------------------------------------------------------------
-- payment_intents : intention de paiement (avant confirmation)
-- Sert à gérer les étapes (3DS, etc.)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS payment_intents (
    id                    uuid PRIMARY KEY DEFAULT uuid_generate_v4(),

    user_id                uuid NOT NULL,
    provider              varchar(30) NOT NULL DEFAULT 'stripe',
    provider_intent_id     text NOT NULL, -- ex: pi_...

    amount                 numeric(10,2) NOT NULL,
    currency               varchar(3) NOT NULL DEFAULT 'EUR',

    status                 varchar(20) NOT NULL DEFAULT 'created',
    -- created/requires_action/requires_payment_method/processing/succeeded/canceled/failed

    purpose_type           varchar(30) NOT NULL, -- subscription/credits/ads_boost/donation/other
    purpose_id             uuid,                 -- id externe (subscription_id, reservation_id, campaign_id)

    client_secret_hash     text, -- optionnel (ne pas stocker le client_secret en clair)
    metadata               jsonb,

    created_at             timestamptz NOT NULL DEFAULT now(),
    updated_at             timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_payment_intents_provider
ON payment_intents (provider, provider_intent_id);

CREATE INDEX IF NOT EXISTS idx_payment_intents_user ON payment_intents (user_id);
CREATE INDEX IF NOT EXISTS idx_payment_intents_status ON payment_intents (status);

------------------------------------------------------------
-- payments : paiements confirmés (source de vérité financière)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS payments (
    id                    uuid PRIMARY KEY DEFAULT uuid_generate_v4(),

    user_id                uuid NOT NULL,

    provider              varchar(30) NOT NULL DEFAULT 'stripe',
    provider_charge_id     text,         -- ex: ch_... (Stripe Charge) ou équivalent
    provider_payment_id    text,         -- ex: pi_... (Stripe PaymentIntent) ou transaction PayPal

    amount                 numeric(10,2) NOT NULL,
    currency               varchar(3) NOT NULL DEFAULT 'EUR',

    status                 varchar(20) NOT NULL DEFAULT 'succeeded',
    -- succeeded/failed/refunded/partially_refunded/disputed

    purpose_type           varchar(30) NOT NULL, -- subscription/credits/ads_boost/donation/other
    purpose_id             uuid,

    payment_method_id      uuid,
    receipt_url            text,

    created_at             timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_payments_method
        FOREIGN KEY (payment_method_id) REFERENCES payment_methods(id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_payments_user ON payments (user_id);
CREATE INDEX IF NOT EXISTS idx_payments_purpose ON payments (purpose_type, purpose_id);
CREATE INDEX IF NOT EXISTS idx_payments_status ON payments (status);

------------------------------------------------------------
-- refunds : remboursements
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS refunds (
    id                    uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    payment_id             uuid NOT NULL,

    provider              varchar(30) NOT NULL DEFAULT 'stripe',
    provider_refund_id     text, -- re_... etc.

    amount                 numeric(10,2) NOT NULL,
    currency               varchar(3) NOT NULL DEFAULT 'EUR',
    status                 varchar(20) NOT NULL DEFAULT 'pending', -- pending/succeeded/failed
    reason                 text,

    created_at             timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_refunds_payment
        FOREIGN KEY (payment_id) REFERENCES payments(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_refunds_payment ON refunds (payment_id);

------------------------------------------------------------
-- webhook_events : stockage des webhooks pour idempotence + debug
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS webhook_events (
    id                    bigserial PRIMARY KEY,

    provider              varchar(30) NOT NULL DEFAULT 'stripe',
    provider_event_id     text NOT NULL, -- evt_... / paypal event id

    event_type            varchar(200) NOT NULL,
    received_at           timestamptz NOT NULL DEFAULT now(),

    payload               jsonb NOT NULL,
    processed_at          timestamptz,
    status                varchar(20) NOT NULL DEFAULT 'pending', -- pending/processed/failed
    error                 text
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_webhook_provider_event
ON webhook_events (provider, provider_event_id);

CREATE INDEX IF NOT EXISTS idx_webhook_status ON webhook_events (status);

------------------------------------------------------------
-- outbox_messages : events sortants vers RabbitMQ
-- ex: Payment.Succeeded, Payment.Failed, Refund.Succeeded
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS outbox_messages (
    id             bigserial PRIMARY KEY,
    message_id     uuid NOT NULL UNIQUE,
    aggregate_type varchar(100),
    aggregate_id   uuid,
    type           varchar(200) NOT NULL,
    payload        jsonb NOT NULL,
    occurred_on    timestamptz NOT NULL DEFAULT now(),
    processed_on   timestamptz,
    status         varchar(20) NOT NULL DEFAULT 'pending',
    error          text
);

CREATE INDEX IF NOT EXISTS idx_outbox_status ON outbox_messages (status);
CREATE INDEX IF NOT EXISTS idx_outbox_occurred_on ON outbox_messages (occurred_on);
