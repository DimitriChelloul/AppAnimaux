-- 01-review-schema.sql
-- Schéma du ReviewService (review_db)
connect review_db

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

------------------------------------------------------------
-- reviews : avis entre utilisateurs (après une aide / un match)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS reviews (
    id                 uuid PRIMARY KEY DEFAULT uuid_generate_v4(),

    -- contexte métier (référence externe)
    help_match_id      uuid NOT NULL,     -- HelpRequestService.help_matches.id (pas de FK inter-db)

    -- qui note qui
    reviewer_user_id   uuid NOT NULL,     -- auteur de l'avis (IdentityService)
    reviewee_user_id   uuid NOT NULL,     -- personne évaluée (IdentityService)

    -- optionnel : si l'avis concerne un animal
    pet_id             uuid,              -- PetService.pets.id (pas de FK inter-db)

    rating             smallint NOT NULL CHECK (rating >= 1 AND rating <= 5),
    comment            text,

    is_public          boolean NOT NULL DEFAULT true,

    -- modération (AdminService)
    moderation_status  varchar(20) NOT NULL DEFAULT 'published', -- published/hidden/flagged/deleted
    moderated_by       uuid,              -- admin user id (IdentityService)
    moderated_at       timestamptz,
    moderation_reason  text,

    created_at         timestamptz NOT NULL DEFAULT now(),
    updated_at         timestamptz NOT NULL DEFAULT now()
);

-- 1 avis par reviewer pour un match donné (évite doublons)
CREATE UNIQUE INDEX IF NOT EXISTS uq_reviews_match_reviewer
ON reviews (help_match_id, reviewer_user_id);

-- Index utiles
CREATE INDEX IF NOT EXISTS idx_reviews_reviewee ON reviews (reviewee_user_id);
CREATE INDEX IF NOT EXISTS idx_reviews_reviewer ON reviews (reviewer_user_id);
CREATE INDEX IF NOT EXISTS idx_reviews_match ON reviews (help_match_id);
CREATE INDEX IF NOT EXISTS idx_reviews_status ON reviews (moderation_status);
CREATE INDEX IF NOT EXISTS idx_reviews_created_at ON reviews (created_at);

------------------------------------------------------------
-- review_flags : signalements d'avis (utilisateurs)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS review_flags (
    id               uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    review_id        uuid NOT NULL,

    flagged_by_user_id uuid NOT NULL,   -- IdentityService
    reason           varchar(50) NOT NULL, -- spam/harassment/fake/other
    details          text,

    status           varchar(20) NOT NULL DEFAULT 'open', -- open/reviewed/dismissed
    reviewed_by      uuid,              -- admin id
    reviewed_at      timestamptz,
    decision_notes   text,

    created_at       timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_review_flags_review
        FOREIGN KEY (review_id) REFERENCES reviews(id) ON DELETE CASCADE
);

-- Eviter 10 signalements du meme user sur le meme avis
CREATE UNIQUE INDEX IF NOT EXISTS uq_review_flags_review_user
ON review_flags (review_id, flagged_by_user_id);

CREATE INDEX IF NOT EXISTS idx_review_flags_status ON review_flags (status);
CREATE INDEX IF NOT EXISTS idx_review_flags_review ON review_flags (review_id);

------------------------------------------------------------
-- outbox_messages : pattern outbox (events)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS outbox_messages (
    id             bigserial PRIMARY KEY,
    message_id     uuid NOT NULL UNIQUE,
    aggregate_type varchar(100),
    aggregate_id   uuid,
    type           varchar(200) NOT NULL, -- ex: Review.Created / Review.Flagged / Review.Moderated
    payload        jsonb NOT NULL,
    occurred_on    timestamptz NOT NULL DEFAULT now(),
    processed_on   timestamptz,
    status         varchar(20) NOT NULL DEFAULT 'pending', -- pending/processed/failed
    error          text
);

CREATE INDEX IF NOT EXISTS idx_outbox_status ON outbox_messages (status);
CREATE INDEX IF NOT EXISTS idx_outbox_occurred_on ON outbox_messages (occurred_on);
