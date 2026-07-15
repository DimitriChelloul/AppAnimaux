-- Harmonise les tables Outbox après la création de tous les schémas métier.

connect identity_db
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_on timestamptz;

connect userprofile_db
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_on timestamptz;

connect pet_db
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_on timestamptz;

connect helprequest_db
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_on timestamptz;

connect review_db
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_on timestamptz;

connect notification_db
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_on timestamptz;

connect privatemessaging_db
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_on timestamptz;

connect forum_db
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_on timestamptz;

connect media_db
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_on timestamptz;

connect admin_db
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_on timestamptz;

connect reporting_db
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_on timestamptz;

connect payment_db
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_on timestamptz;

connect credit_db
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_on timestamptz;

connect advertising_db
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_on timestamptz;

connect subscription_db
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_on timestamptz;

connect location_db
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_on timestamptz;

connect alert_db
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_on timestamptz;

connect professional_db
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_on timestamptz;

connect chatbot_db
CREATE TABLE IF NOT EXISTS outbox_messages (
    id bigserial PRIMARY KEY,
    message_id uuid NOT NULL UNIQUE,
    aggregate_type varchar(100),
    aggregate_id uuid,
    type varchar(200) NOT NULL,
    payload jsonb NOT NULL,
    occurred_on timestamptz NOT NULL DEFAULT now(),
    processed_on timestamptz,
    status varchar(20) NOT NULL DEFAULT 'pending',
    error text,
    attempts integer NOT NULL DEFAULT 0,
    next_attempt_on timestamptz
);
CREATE INDEX IF NOT EXISTS idx_chatbot_outbox_status_attempt
    ON outbox_messages(status, next_attempt_on, occurred_on);
