-- 01-privatemessaging-schema.sql
-- Schéma du PrivateMessagingService (privatemessaging_db)
connect privatemessaging_db

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

------------------------------------------------------------
-- conversations : 1 conversation (DM ou groupe)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS conversations (
    id               uuid PRIMARY KEY DEFAULT uuid_generate_v4(),

    type             varchar(20) NOT NULL DEFAULT 'dm', -- dm/group
    title            text,                              -- pour group
    created_by_user_id uuid NOT NULL,                   -- IdentityService (pas de FK inter-db)

    last_message_at  timestamptz,
    last_message_id  uuid,

    is_archived      boolean NOT NULL DEFAULT false,

    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_conversations_last_message_at
ON conversations (last_message_at DESC);

------------------------------------------------------------
-- conversation_members : membres d'une conversation
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS conversation_members (
    conversation_id  uuid NOT NULL,
    user_id          uuid NOT NULL, -- IdentityService

    role             varchar(20) NOT NULL DEFAULT 'member', -- owner/admin/member
    joined_at        timestamptz NOT NULL DEFAULT now(),

    -- gestion "archive/mute" par utilisateur
    is_muted         boolean NOT NULL DEFAULT false,
    muted_until      timestamptz,

    is_hidden        boolean NOT NULL DEFAULT false, -- masquer la conversation
    hidden_at        timestamptz,

    -- tracking lecture
    last_read_message_id uuid,
    last_read_at     timestamptz,

    PRIMARY KEY (conversation_id, user_id),
    CONSTRAINT fk_members_conversation
        FOREIGN KEY (conversation_id) REFERENCES conversations(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_members_user_id ON conversation_members (user_id);

------------------------------------------------------------
-- messages : messages envoyes dans une conversation
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS messages (
    id               uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    conversation_id  uuid NOT NULL,

    sender_user_id   uuid NOT NULL, -- IdentityService
    message_type     varchar(20) NOT NULL DEFAULT 'text', -- text/image/file/system
    content          text, -- texte (si text)

    -- pieces jointes via MediaService (metadata seulement)
    attachments      jsonb, -- ex: [{ "mediaId":"...", "url":"...", "contentType":"image/png" }]

    -- moderation/soft delete
    is_deleted       boolean NOT NULL DEFAULT false,
    deleted_at       timestamptz,
    deleted_by_user_id uuid,
    delete_reason    text,

    created_at       timestamptz NOT NULL DEFAULT now(),
    edited_at        timestamptz,

    CONSTRAINT fk_messages_conversation
        FOREIGN KEY (conversation_id) REFERENCES conversations(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_messages_conversation_created
ON messages (conversation_id, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_messages_sender
ON messages (sender_user_id);

------------------------------------------------------------
-- message_receipts : "delivered/read" par message (optionnel)
-- Utile si tu veux un suivi fin par message. Sinon, last_read_at suffit.
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS message_receipts (
    message_id       uuid NOT NULL,
    user_id          uuid NOT NULL,

    delivered_at     timestamptz,
    read_at          timestamptz,

    PRIMARY KEY (message_id, user_id),

    CONSTRAINT fk_receipts_message
        FOREIGN KEY (message_id) REFERENCES messages(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_receipts_user_id ON message_receipts (user_id);

------------------------------------------------------------
-- conversation_events : audit metier (facultatif mais pratique)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS conversation_events (
    id              bigserial PRIMARY KEY,
    conversation_id uuid NOT NULL,
    event_type      varchar(50) NOT NULL, -- created/member_added/member_removed/title_changed/etc.
    data            jsonb,
    created_at      timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_conv_events_conversation
        FOREIGN KEY (conversation_id) REFERENCES conversations(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_conv_events_conversation
ON conversation_events (conversation_id);

------------------------------------------------------------
-- outbox_messages : events sortants vers RabbitMQ
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS outbox_messages (
    id             bigserial PRIMARY KEY,
    message_id     uuid NOT NULL UNIQUE,
    aggregate_type varchar(100),
    aggregate_id   uuid,
    type           varchar(200) NOT NULL, -- ex: Messaging.MessageSent
    payload        jsonb NOT NULL,
    occurred_on    timestamptz NOT NULL DEFAULT now(),
    processed_on   timestamptz,
    status         varchar(20) NOT NULL DEFAULT 'pending', -- pending/processed/failed
    error          text,
    attempts       integer NOT NULL DEFAULT 0,
    next_attempt_on timestamptz
);

CREATE INDEX IF NOT EXISTS idx_outbox_status ON outbox_messages (status);
CREATE INDEX IF NOT EXISTS idx_outbox_occurred_on ON outbox_messages (occurred_on);
