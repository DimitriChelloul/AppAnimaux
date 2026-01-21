-- 01-credit-schema.sql
-- Schéma du CreditService (credit_db)
-- Objectif: portefeuille crédits, mouvements, et consommation

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

------------------------------------------------------------
-- wallets : 1 wallet par utilisateur
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS wallets (
    id            uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id        uuid NOT NULL UNIQUE, -- IdentityService
    balance        bigint NOT NULL DEFAULT 0,
    updated_at     timestamptz NOT NULL DEFAULT now(),
    created_at     timestamptz NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- credit_transactions : ledger (source de vérité)
-- On ne modifie jamais une transaction: append-only.
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS credit_transactions (
    id               uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    wallet_id         uuid NOT NULL,

    tx_type          varchar(20) NOT NULL, -- grant/purchase/spend/refund/adjustment
    amount           bigint NOT NULL,       -- +100 / -20
    reason_code      varchar(50),           -- monthly_grant/boost_listing/message_fee/...
    reference_type   varchar(50),           -- subscription/payment/help_request/...
    reference_id     uuid,                  -- id externe

    description      text,
    created_at       timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_tx_wallet
        FOREIGN KEY (wallet_id) REFERENCES wallets(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_tx_wallet_time ON credit_transactions (wallet_id, created_at DESC);

------------------------------------------------------------
-- credit_reservations : éviter double dépense (réservation temporaire)
-- exemple: "booster une annonce" -> reserve -> confirm ou cancel
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS credit_reservations (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    wallet_id        uuid NOT NULL,

    amount           bigint NOT NULL,
    status           varchar(20) NOT NULL DEFAULT 'reserved', -- reserved/confirmed/canceled/expired

    reference_type   varchar(50),
    reference_id     uuid,

    expires_at       timestamptz NOT NULL,
    created_at       timestamptz NOT NULL DEFAULT now(),
    confirmed_at     timestamptz,
    canceled_at      timestamptz,

    CONSTRAINT fk_res_wallet
        FOREIGN KEY (wallet_id) REFERENCES wallets(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_res_wallet_status ON credit_reservations (wallet_id, status);
CREATE INDEX IF NOT EXISTS idx_res_expires ON credit_reservations (expires_at);

------------------------------------------------------------
-- outbox_messages
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS outbox_messages (
    id             bigserial PRIMARY KEY,
    message_id     uuid NOT NULL UNIQUE,
    aggregate_type varchar(100),
    aggregate_id   uuid,
    type           varchar(200) NOT NULL, -- Credits.Granted / Credits.Spent ...
    payload        jsonb NOT NULL,
    occurred_on    timestamptz NOT NULL DEFAULT now(),
    processed_on   timestamptz,
    status         varchar(20) NOT NULL DEFAULT 'pending',
    error          text
);

CREATE INDEX IF NOT EXISTS idx_outbox_status ON outbox_messages (status);
CREATE INDEX IF NOT EXISTS idx_outbox_occurred_on ON outbox_messages (occurred_on);
