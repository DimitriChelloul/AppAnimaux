-- 01-notification-schema.sql
-- Schéma du NotificationService (notification_db)
connect notification_db

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "citext";

------------------------------------------------------------
-- user_notification_settings : preferences par utilisateur
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS user_notification_settings (
    id                     uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id                uuid NOT NULL UNIQUE, -- IdentityService (pas de FK inter-db)

    email_enabled          boolean NOT NULL DEFAULT true,
    push_enabled           boolean NOT NULL DEFAULT true,
    inapp_enabled          boolean NOT NULL DEFAULT true,

    marketing_enabled      boolean NOT NULL DEFAULT false,

    quiet_hours_enabled    boolean NOT NULL DEFAULT false,
    quiet_hours_start      time,
    quiet_hours_end        time,
    timezone               varchar(50) DEFAULT 'Europe/Paris',

    created_at             timestamptz NOT NULL DEFAULT now(),
    updated_at             timestamptz NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- notification_channels : "endpoints" ou canaux (email, push token, etc.)
-- On garde tout ce qui permet d'atteindre l'utilisateur.
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS notification_channels (
    id            uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id        uuid NOT NULL,

    channel_type   varchar(20) NOT NULL, -- email/push/sms (sms optionnel)
    destination    citext NOT NULL,      -- email ou phone; pour push on stocke "device:<id>" ou un token
    provider       varchar(50),          -- firebase/apns/sendgrid/twilio/etc.
    is_active      boolean NOT NULL DEFAULT true,
    is_verified    boolean NOT NULL DEFAULT false,
    verified_at    timestamptz,

    metadata       jsonb,                -- ex: { "deviceId": "...", "platform": "android", "appVersion": "1.0.0" }

    created_at     timestamptz NOT NULL DEFAULT now(),
    updated_at     timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_channels_user_id ON notification_channels (user_id);
CREATE INDEX IF NOT EXISTS idx_channels_type ON notification_channels (channel_type);
CREATE UNIQUE INDEX IF NOT EXISTS uq_channels_type_destination
ON notification_channels (channel_type, destination);

------------------------------------------------------------
-- device_push_tokens : tokens push (Firebase/APNS) par device
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS device_push_tokens (
    id            uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id        uuid NOT NULL,

    platform      varchar(20) NOT NULL, -- android/ios/web
    device_id     varchar(128) NOT NULL,
    push_token    text NOT NULL,        -- token FCM/APNS
    is_active     boolean NOT NULL DEFAULT true,

    last_seen_at  timestamptz,
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_push_tokens_user_id ON device_push_tokens (user_id);
CREATE UNIQUE INDEX IF NOT EXISTS uq_push_tokens_platform_device
ON device_push_tokens (platform, device_id);

------------------------------------------------------------
-- notifications : notifications "in-app" + tracking envoi
-- Ce sont les notifications visibles dans l'app (historique).
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS notifications (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id          uuid NOT NULL,

    title           text,
    body            text NOT NULL,
    notification_type varchar(80) NOT NULL, -- HelpRequest.Created, Message.Received, Review.Received...
    data            jsonb,                  -- payload "deep link", ids, etc.

    priority        varchar(10) NOT NULL DEFAULT 'normal', -- low/normal/high
    is_read         boolean NOT NULL DEFAULT false,
    read_at         timestamptz,

    created_at      timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_notifications_user_created ON notifications (user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_notifications_unread ON notifications (user_id, is_read);

------------------------------------------------------------
-- notification_delivery : journal d'envoi par canal
-- Permet de savoir si email/push a ete envoye / echoue.
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS notification_delivery (
    id               bigserial PRIMARY KEY,
    notification_id  uuid NOT NULL,
    channel_type     varchar(20) NOT NULL, -- email/push/sms/inapp
    destination      citext,
    provider         varchar(50),
    status           varchar(20) NOT NULL DEFAULT 'pending', -- pending/sent/failed
    provider_message_id text,
    error            text,
    attempt_count    int NOT NULL DEFAULT 0,
    next_attempt_at  timestamptz,
    sent_at          timestamptz,
    created_at       timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_delivery_notification
        FOREIGN KEY (notification_id) REFERENCES notifications(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_delivery_status ON notification_delivery (status);
CREATE INDEX IF NOT EXISTS idx_delivery_next_attempt ON notification_delivery (next_attempt_at);

------------------------------------------------------------
-- outbox_messages : events sortants (vers RabbitMQ)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS outbox_messages (
    id             bigserial PRIMARY KEY,
    message_id     uuid NOT NULL UNIQUE,
    aggregate_type varchar(100),
    aggregate_id   uuid,
    type           varchar(200) NOT NULL, -- ex: Notification.Sent / Notification.Failed
    payload        jsonb NOT NULL,
    occurred_on    timestamptz NOT NULL DEFAULT now(),
    processed_on   timestamptz,
    status         varchar(20) NOT NULL DEFAULT 'pending', -- pending/processed/failed
    error          text
);

CREATE INDEX IF NOT EXISTS idx_outbox_status ON outbox_messages (status);
CREATE INDEX IF NOT EXISTS idx_outbox_occurred_on ON outbox_messages (occurred_on);
