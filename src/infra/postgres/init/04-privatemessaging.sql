SELECT 'CREATE DATABASE privatemessaging_db'
WHERE NOT EXISTS (
    SELECT FROM pg_database WHERE datname = 'privatemessaging_db'
)\gexec

\connect privatemessaging_db

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS conversations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    created_by_user_id uuid NOT NULL,
    type text NOT NULL DEFAULT 'dm',
    title text NULL,
    last_message_id uuid NULL,
    last_message_at timestamptz NULL,
    is_archived boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_conversations_type CHECK (type IN ('dm', 'group', 'support'))
);

CREATE TABLE IF NOT EXISTS conversation_members (
    conversation_id uuid NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    user_id uuid NOT NULL,
    role text NOT NULL DEFAULT 'member',
    last_read_message_id uuid NULL,
    last_read_at timestamptz NULL,
    is_hidden boolean NOT NULL DEFAULT false,
    joined_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (conversation_id, user_id),
    CONSTRAINT ck_conversation_members_role CHECK (role IN ('owner', 'member', 'admin'))
);

CREATE TABLE IF NOT EXISTS messages (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    conversation_id uuid NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    sender_user_id uuid NOT NULL,
    message_type text NOT NULL DEFAULT 'text',
    content text NULL,
    attachments jsonb NOT NULL DEFAULT '[]'::jsonb,
    is_deleted boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    edited_at timestamptz NULL,
    CONSTRAINT ck_messages_message_type CHECK (message_type IN ('text', 'image', 'system'))
);

ALTER TABLE conversations
    DROP CONSTRAINT IF EXISTS fk_conversations_last_message;

ALTER TABLE conversations
    ADD CONSTRAINT fk_conversations_last_message
    FOREIGN KEY (last_message_id) REFERENCES messages(id) ON DELETE SET NULL;

ALTER TABLE conversation_members
    DROP CONSTRAINT IF EXISTS fk_conversation_members_last_read_message;

ALTER TABLE conversation_members
    ADD CONSTRAINT fk_conversation_members_last_read_message
    FOREIGN KEY (last_read_message_id) REFERENCES messages(id) ON DELETE SET NULL;

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

CREATE INDEX IF NOT EXISTS ix_conversations_created_by ON conversations (created_by_user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_conversations_last_message_at ON conversations (last_message_at DESC NULLS LAST);
CREATE INDEX IF NOT EXISTS ix_conversation_members_user ON conversation_members (user_id, is_hidden);
CREATE INDEX IF NOT EXISTS ix_messages_conversation_created ON messages (conversation_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_outbox_messages_status_occurred_on
    ON outbox_messages (status, occurred_on);
