-- 01-forum-schema.sql
-- Schéma du ForumService (forum_db)

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "citext";

------------------------------------------------------------
-- forum_categories : catégories (Announce, Santé, Education, Perte, etc.)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS forum_categories (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    name            citext NOT NULL UNIQUE,
    description     text,
    slug            citext NOT NULL UNIQUE,

    is_locked       boolean NOT NULL DEFAULT false,
    sort_order      int NOT NULL DEFAULT 0,

    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- forum_topics : sujets
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS forum_topics (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),

    category_id     uuid NOT NULL,

    author_user_id  uuid NOT NULL,          -- IdentityService
    title           text NOT NULL,
    slug            citext,                 -- optionnel
    content         text,                   -- 1er message "corps" (option : sinon post)
    attachments     jsonb,                  -- MediaService metadata
    tags            text[],                 -- optionnel

    status          varchar(20) NOT NULL DEFAULT 'open', -- open/locked/hidden/deleted
    is_pinned       boolean NOT NULL DEFAULT false,

    views_count     bigint NOT NULL DEFAULT 0,
    replies_count   bigint NOT NULL DEFAULT 0,
    last_post_at    timestamptz,

    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_topics_category
        FOREIGN KEY (category_id) REFERENCES forum_categories(id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS idx_topics_category_created
ON forum_topics (category_id, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_topics_author
ON forum_topics (author_user_id);

CREATE INDEX IF NOT EXISTS idx_topics_status
ON forum_topics (status);

CREATE INDEX IF NOT EXISTS idx_topics_last_post_at
ON forum_topics (last_post_at DESC);

------------------------------------------------------------
-- forum_posts : réponses dans un topic
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS forum_posts (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),

    topic_id         uuid NOT NULL,
    author_user_id   uuid NOT NULL,       -- IdentityService

    content          text NOT NULL,
    attachments      jsonb,               -- MediaService metadata

    status           varchar(20) NOT NULL DEFAULT 'published', -- published/hidden/deleted
    edited_at        timestamptz,

    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_posts_topic
        FOREIGN KEY (topic_id) REFERENCES forum_topics(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_posts_topic_created
ON forum_posts (topic_id, created_at);

CREATE INDEX IF NOT EXISTS idx_posts_author
ON forum_posts (author_user_id);

CREATE INDEX IF NOT EXISTS idx_posts_status
ON forum_posts (status);

------------------------------------------------------------
-- forum_reactions : likes/upvotes sur topics ou posts
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS forum_reactions (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),

    user_id         uuid NOT NULL,     -- IdentityService
    target_type     varchar(10) NOT NULL CHECK (target_type IN ('topic','post')),
    target_id       uuid NOT NULL,

    reaction_type   varchar(20) NOT NULL DEFAULT 'like', -- like/upvote/etc.

    created_at      timestamptz NOT NULL DEFAULT now()
);

-- Un utilisateur ne peut reagir qu'une fois par cible et type de reaction
CREATE UNIQUE INDEX IF NOT EXISTS uq_reactions_user_target
ON forum_reactions (user_id, target_type, target_id, reaction_type);

CREATE INDEX IF NOT EXISTS idx_reactions_target
ON forum_reactions (target_type, target_id);

------------------------------------------------------------
-- forum_flags : signalement de topic/post
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS forum_flags (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),

    flagged_by_user_id uuid NOT NULL, -- IdentityService
    target_type     varchar(10) NOT NULL CHECK (target_type IN ('topic','post')),
    target_id       uuid NOT NULL,

    reason          varchar(50) NOT NULL, -- spam/harassment/hate/illegal/other
    details         text,

    status          varchar(20) NOT NULL DEFAULT 'open', -- open/reviewed/dismissed
    reviewed_by     uuid,               -- admin id (IdentityService)
    reviewed_at     timestamptz,
    decision_notes  text,

    created_at      timestamptz NOT NULL DEFAULT now()
);

-- Evite qu'un user spamme le signalement sur le meme contenu
CREATE UNIQUE INDEX IF NOT EXISTS uq_flags_user_target
ON forum_flags (flagged_by_user_id, target_type, target_id);

CREATE INDEX IF NOT EXISTS idx_flags_status
ON forum_flags (status);

CREATE INDEX IF NOT EXISTS idx_flags_target
ON forum_flags (target_type, target_id);

------------------------------------------------------------
-- outbox_messages : events sortants vers RabbitMQ
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS outbox_messages (
    id             bigserial PRIMARY KEY,
    message_id     uuid NOT NULL UNIQUE,
    aggregate_type varchar(100),
    aggregate_id   uuid,
    type           varchar(200) NOT NULL, -- ex: Forum.TopicCreated / Forum.PostCreated / Forum.Flagged
    payload        jsonb NOT NULL,
    occurred_on    timestamptz NOT NULL DEFAULT now(),
    processed_on   timestamptz,
    status         varchar(20) NOT NULL DEFAULT 'pending', -- pending/processed/failed
    error          text
);

CREATE INDEX IF NOT EXISTS idx_outbox_status ON outbox_messages (status);
CREATE INDEX IF NOT EXISTS idx_outbox_occurred_on ON outbox_messages (occurred_on);
