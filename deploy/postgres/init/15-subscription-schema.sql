-- 01-subscription-schema.sql
-- Schéma du SubscriptionService (subscription_db)
connect subscription_db

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

------------------------------------------------------------
-- plans : catalog des offres (Free, Basic, Premium, Pro...)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS plans (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    code            varchar(50) NOT NULL UNIQUE,   -- FREE/BASIC/PREMIUM/PRO
    name            text NOT NULL,
    description     text,

    price_amount    numeric(10,2) NOT NULL DEFAULT 0,
    currency        varchar(3) NOT NULL DEFAULT 'EUR',
    period          varchar(20) NOT NULL DEFAULT 'monthly', -- monthly/yearly/one_time

    is_active       boolean NOT NULL DEFAULT true,
    features        jsonb, -- ex: {"adsFree":true,"monthlyCredits":100,"prioritySupport":true}

    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- subscriptions : abonnement actif/historique
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS subscriptions (
    id                 uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id             uuid NOT NULL, -- IdentityService

    plan_id             uuid NOT NULL,
    status              varchar(20) NOT NULL DEFAULT 'active', -- trialing/active/past_due/canceled/expired

    start_at            timestamptz NOT NULL DEFAULT now(),
    current_period_start timestamptz NOT NULL DEFAULT now(),
    current_period_end  timestamptz NOT NULL,
    cancel_at_period_end boolean NOT NULL DEFAULT false,
    canceled_at         timestamptz,

    -- lien paiement (PaymentService)
    payment_provider    varchar(50),     -- stripe/paypal/...
    provider_customer_id text,
    provider_subscription_id text,

    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_subscriptions_plan
        FOREIGN KEY (plan_id) REFERENCES plans(id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS idx_subscriptions_user ON subscriptions (user_id);
CREATE INDEX IF NOT EXISTS idx_subscriptions_status ON subscriptions (status);

-- 1 abonnement "actif" max par user (contrainte pragmatique)
CREATE UNIQUE INDEX IF NOT EXISTS uq_subscriptions_active_per_user
ON subscriptions (user_id)
WHERE status IN ('trialing','active','past_due');

------------------------------------------------------------
-- subscription_invoices : trace des factures / cycles
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS subscription_invoices (
    id                 uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    subscription_id    uuid NOT NULL,

    period_start       timestamptz NOT NULL,
    period_end         timestamptz NOT NULL,

    amount             numeric(10,2) NOT NULL,
    currency           varchar(3) NOT NULL DEFAULT 'EUR',

    status             varchar(20) NOT NULL DEFAULT 'pending', -- pending/paid/failed/refunded
    provider_invoice_id text,
    paid_at            timestamptz,

    created_at         timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_invoices_subscription
        FOREIGN KEY (subscription_id) REFERENCES subscriptions(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_invoices_subscription ON subscription_invoices (subscription_id);
CREATE INDEX IF NOT EXISTS idx_invoices_status ON subscription_invoices (status);

------------------------------------------------------------
-- outbox_messages
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS outbox_messages (
    id             bigserial PRIMARY KEY,
    message_id     uuid NOT NULL UNIQUE,
    aggregate_type varchar(100),
    aggregate_id   uuid,
    type           varchar(200) NOT NULL, -- Subscription.Activated / Subscription.Canceled ...
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

CREATE TABLE IF NOT EXISTS inbox_messages (
    message_id  uuid PRIMARY KEY,
    event_type  varchar(200) NOT NULL,
    processed_on timestamptz NOT NULL DEFAULT now()
);
