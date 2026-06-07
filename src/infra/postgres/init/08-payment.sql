SELECT 'CREATE DATABASE payment_db'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'payment_db')\gexec

\connect payment_db

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS subscription_plans (
    id uuid PRIMARY KEY,
    code text NOT NULL UNIQUE,
    name text NOT NULL,
    owner_type text NOT NULL CHECK (owner_type IN ('User', 'Professional')),
    provider text NULL CHECK (provider IS NULL OR provider IN ('Apple', 'Google', 'Stripe')),
    price_amount numeric(12,2) NOT NULL DEFAULT 0,
    currency char(3) NOT NULL DEFAULT 'EUR',
    billing_period text NOT NULL DEFAULT 'month',
    stripe_price_id text NULL,
    apple_product_id text NULL,
    google_product_id text NULL,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS user_subscriptions (
    id uuid PRIMARY KEY,
    user_id uuid NOT NULL,
    plan_id uuid NOT NULL REFERENCES subscription_plans(id),
    provider text NOT NULL CHECK (provider IN ('Apple', 'Google', 'Stripe')),
    external_subscription_id text NULL,
    external_customer_id text NULL,
    status text NOT NULL CHECK (status IN ('Pending', 'Active', 'PastDue', 'Canceled', 'Expired', 'Failed')),
    current_period_start timestamptz NULL,
    current_period_end timestamptz NULL,
    auto_renew boolean NOT NULL DEFAULT true,
    canceled_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_user_subscriptions_user_id ON user_subscriptions(user_id);
CREATE UNIQUE INDEX IF NOT EXISTS ux_user_subscriptions_provider_external
    ON user_subscriptions(provider, external_subscription_id)
    WHERE external_subscription_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS professional_subscriptions (
    id uuid PRIMARY KEY,
    professional_id uuid NOT NULL,
    plan_id uuid NOT NULL REFERENCES subscription_plans(id),
    stripe_customer_id text NULL,
    stripe_subscription_id text NULL,
    status text NOT NULL CHECK (status IN ('Pending', 'Active', 'PastDue', 'Canceled', 'Expired', 'Failed')),
    current_period_start timestamptz NULL,
    current_period_end timestamptz NULL,
    auto_renew boolean NOT NULL DEFAULT true,
    canceled_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_professional_subscriptions_professional_id
    ON professional_subscriptions(professional_id);
CREATE UNIQUE INDEX IF NOT EXISTS ux_professional_subscriptions_stripe_subscription
    ON professional_subscriptions(stripe_subscription_id)
    WHERE stripe_subscription_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS payment_provider_customers (
    id uuid PRIMARY KEY,
    owner_type text NOT NULL CHECK (owner_type IN ('User', 'Professional')),
    owner_id uuid NOT NULL,
    provider text NOT NULL CHECK (provider IN ('Apple', 'Google', 'Stripe')),
    external_customer_id text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(owner_type, owner_id, provider)
);

CREATE TABLE IF NOT EXISTS subscription_entitlements (
    id uuid PRIMARY KEY,
    plan_id uuid NOT NULL REFERENCES subscription_plans(id) ON DELETE CASCADE,
    key text NOT NULL,
    value text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE(plan_id, key)
);

CREATE TABLE IF NOT EXISTS subscription_invoices (
    id uuid PRIMARY KEY,
    subscription_owner_type text NOT NULL CHECK (subscription_owner_type IN ('User', 'Professional')),
    subscription_id uuid NOT NULL,
    provider text NOT NULL CHECK (provider IN ('Apple', 'Google', 'Stripe')),
    external_invoice_id text NULL,
    amount numeric(12,2) NOT NULL DEFAULT 0,
    currency char(3) NOT NULL DEFAULT 'EUR',
    status text NOT NULL CHECK (status IN ('Pending', 'Paid', 'Failed', 'Refunded')),
    invoice_url text NULL,
    paid_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_subscription_invoices_provider_external
    ON subscription_invoices(provider, external_invoice_id)
    WHERE external_invoice_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS external_purchase_receipts (
    id uuid PRIMARY KEY,
    user_id uuid NOT NULL,
    provider text NOT NULL CHECK (provider IN ('Apple', 'Google', 'Stripe')),
    product_id text NOT NULL,
    transaction_id text NULL,
    original_transaction_id text NULL,
    purchase_token text NULL,
    raw_receipt jsonb NOT NULL DEFAULT '{}'::jsonb,
    validation_status text NOT NULL,
    expires_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_external_purchase_receipts_user_id ON external_purchase_receipts(user_id);

CREATE TABLE IF NOT EXISTS webhook_events (
    id uuid PRIMARY KEY,
    provider text NOT NULL CHECK (provider IN ('Apple', 'Google', 'Stripe')),
    event_type text NOT NULL,
    external_event_id text NULL,
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    processed boolean NOT NULL DEFAULT false,
    processed_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_webhook_events_provider_external
    ON webhook_events(provider, external_event_id)
    WHERE external_event_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS subscription_event_logs (
    id uuid PRIMARY KEY,
    owner_type text NOT NULL CHECK (owner_type IN ('User', 'Professional')),
    owner_id uuid NOT NULL,
    event_type text NOT NULL,
    details jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS payment_audit_logs (
    id uuid PRIMARY KEY,
    owner_type text NOT NULL CHECK (owner_type IN ('User', 'Professional')),
    owner_id uuid NOT NULL,
    action text NOT NULL,
    provider text NULL CHECK (provider IS NULL OR provider IN ('Apple', 'Google', 'Stripe')),
    details jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS payments (
    id uuid PRIMARY KEY,
    user_id uuid NOT NULL,
    provider text NOT NULL,
    provider_payment_id text NOT NULL,
    amount numeric(12,2) NOT NULL,
    currency char(3) NOT NULL,
    status text NOT NULL,
    purpose_type text NOT NULL,
    purpose_id uuid NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS outbox_messages (
    message_id uuid PRIMARY KEY,
    aggregate_type text NULL,
    aggregate_id uuid NULL,
    type text NOT NULL,
    payload jsonb NOT NULL,
    occurred_on timestamptz NOT NULL DEFAULT now(),
    status text NOT NULL DEFAULT 'pending',
    attempts int NOT NULL DEFAULT 0,
    last_error text NULL
);

INSERT INTO subscription_plans(id, code, name, owner_type, provider, price_amount, currency, billing_period, stripe_price_id, apple_product_id, google_product_id)
VALUES
('00000000-0000-0000-0000-000000000101', 'Free', 'Free', 'User', NULL, 0, 'EUR', 'month', NULL, NULL, NULL),
('00000000-0000-0000-0000-000000000102', 'UserPremium', 'User Premium', 'User', NULL, 4.99, 'EUR', 'month', NULL, 'appanimaux.userpremium.monthly', 'userpremium_monthly'),
('00000000-0000-0000-0000-000000000103', 'UserPlus', 'User Plus', 'User', NULL, 7.99, 'EUR', 'month', NULL, 'appanimaux.userplus.monthly', 'userplus_monthly'),
('00000000-0000-0000-0000-000000000201', 'ProFree', 'Pro Free', 'Professional', 'Stripe', 0, 'EUR', 'month', NULL, NULL, NULL),
('00000000-0000-0000-0000-000000000202', 'ProBasic', 'Pro Basic', 'Professional', 'Stripe', 19.00, 'EUR', 'month', 'price_replace_pro_basic', NULL, NULL),
('00000000-0000-0000-0000-000000000203', 'ProPlus', 'Pro Plus', 'Professional', 'Stripe', 39.00, 'EUR', 'month', 'price_replace_pro_plus', NULL, NULL),
('00000000-0000-0000-0000-000000000204', 'ProPremium', 'Pro Premium', 'Professional', 'Stripe', 79.00, 'EUR', 'month', 'price_replace_pro_premium', NULL, NULL)
ON CONFLICT (code) DO NOTHING;

INSERT INTO subscription_entitlements(id, plan_id, key, value)
VALUES
(gen_random_uuid(), '00000000-0000-0000-0000-000000000101', 'max_help_requests_per_month', '3'),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000101', 'chatbot_advanced_enabled', 'false'),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000101', 'ads_disabled', 'false'),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000102', 'max_help_requests_per_month', '20'),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000102', 'chatbot_advanced_enabled', 'true'),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000102', 'ads_disabled', 'true'),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000103', 'max_help_requests_per_month', '50'),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000103', 'chatbot_advanced_enabled', 'true'),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000103', 'priority_support_enabled', 'true'),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000201', 'professional_profile_photos_limit', '1'),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000201', 'professional_contact_visible', 'false'),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000202', 'professional_profile_photos_limit', '5'),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000202', 'professional_contact_visible', 'true'),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000203', 'professional_profile_photos_limit', '20'),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000203', 'professional_directory_boost_level', '2'),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000203', 'professional_stats_enabled', 'true'),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000204', 'professional_profile_photos_limit', '50'),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000204', 'professional_directory_boost_level', '5'),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000204', 'professional_verified_badge_enabled', 'true'),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000204', 'professional_booking_enabled', 'true')
ON CONFLICT (plan_id, key) DO NOTHING;
