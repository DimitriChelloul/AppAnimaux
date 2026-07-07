SELECT 'CREATE DATABASE chatbot_db'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'chatbot_db')\gexec

\connect chatbot_db

CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS chatbot_conversations (
    id uuid PRIMARY KEY,
    user_id uuid NULL,
    title text NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS chatbot_messages (
    id uuid PRIMARY KEY,
    conversation_id uuid NOT NULL REFERENCES chatbot_conversations(id) ON DELETE CASCADE,
    role text NOT NULL,
    content text NOT NULL,
    requires_veterinary_attention boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_chatbot_messages_conversation_created
    ON chatbot_messages (conversation_id, created_at DESC);

CREATE TABLE IF NOT EXISTS chatbot_documents (
    id uuid PRIMARY KEY,
    title text NOT NULL,
    content text NOT NULL,
    source_type text NOT NULL,
    source_uri text NULL,
    locale text NULL,
    status text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_chatbot_documents_status
    ON chatbot_documents (status);

CREATE TABLE IF NOT EXISTS chatbot_chunks (
    id uuid PRIMARY KEY,
    document_id uuid NOT NULL REFERENCES chatbot_documents(id) ON DELETE CASCADE,
    chunk_index integer NOT NULL,
    content text NOT NULL,
    token_estimate integer NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_chatbot_chunks_document_index UNIQUE (document_id, chunk_index)
);

CREATE TABLE IF NOT EXISTS chatbot_chunk_embeddings (
    chunk_id uuid PRIMARY KEY REFERENCES chatbot_chunks(id) ON DELETE CASCADE,
    embedding vector(1536) NOT NULL,
    model text NOT NULL,
    dimensions integer NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_chatbot_chunk_embeddings_vector
    ON chatbot_chunk_embeddings
    USING ivfflat (embedding vector_cosine_ops)
    WITH (lists = 100);

CREATE TABLE IF NOT EXISTS chatbot_feedback (
    id uuid PRIMARY KEY,
    conversation_id uuid NOT NULL REFERENCES chatbot_conversations(id) ON DELETE CASCADE,
    message_id uuid NULL REFERENCES chatbot_messages(id) ON DELETE SET NULL,
    user_id uuid NULL,
    rating integer NULL,
    comment text NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_chatbot_feedback_rating CHECK (rating IS NULL OR rating BETWEEN 1 AND 5)
);
