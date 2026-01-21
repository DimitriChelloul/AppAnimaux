-- 01-admin-schema.sql
-- Schéma du AdminService (admin_db)
-- Objectif: modération, audit admin, sanctions, décisions, files de review

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

------------------------------------------------------------
-- admin_roles : roles d'administration (optionnel si Identity gère déjà)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS admin_roles (
    id          smallserial PRIMARY KEY,
    name        varchar(50) NOT NULL UNIQUE, -- super_admin/moderator/support
    description text,
    created_at  timestamptz NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- admin_users : mapping user -> role admin (si tu veux isoler la partie admin)
-- Sinon: tu relies directement via Identity roles et tu peux supprimer cette table.
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS admin_users (
    id            uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id       uuid NOT NULL UNIQUE, -- IdentityService user id (pas de FK inter-db)
    admin_role_id smallint NOT NULL,
    is_active     boolean NOT NULL DEFAULT true,
    created_at    timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_admin_users_role
        FOREIGN KEY (admin_role_id) REFERENCES admin_roles(id) ON DELETE RESTRICT
);

------------------------------------------------------------
-- moderation_actions : historique des actions de modération (audit métier)
-- Exemple: cacher un post, supprimer un avis, bannir un user, etc.
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS moderation_actions (
    id              bigserial PRIMARY KEY,

    admin_user_id   uuid NOT NULL,     -- IdentityService (ou admin_users.user_id)
    action_type     varchar(50) NOT NULL, -- hide_content/delete_content/ban_user/mute_user/warn_user...
    target_service  varchar(50) NOT NULL, -- ForumService/ReviewService/MessagingService/IdentityService...
    target_type     varchar(50) NOT NULL, -- topic/post/review/message/user...
    target_id       uuid NOT NULL,

    reason_code     varchar(50),       -- spam/harassment/fake/etc.
    reason_details  text,

    decision        varchar(20) NOT NULL DEFAULT 'applied', -- applied/reverted/failed
    metadata        jsonb,             -- payload libre (avant/apres, urls, etc.)

    created_at      timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_mod_actions_target
ON moderation_actions (target_service, target_type, target_id);

CREATE INDEX IF NOT EXISTS idx_mod_actions_admin
ON moderation_actions (admin_user_id);

CREATE INDEX IF NOT EXISTS idx_mod_actions_created
ON moderation_actions (created_at DESC);

------------------------------------------------------------
-- user_sanctions : sanctions sur un utilisateur (ban/mute/limited)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS user_sanctions (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),

    user_id          uuid NOT NULL,      -- IdentityService user
    imposed_by_admin uuid NOT NULL,      -- admin user id (Identity)
    sanction_type    varchar(20) NOT NULL, -- ban/mute/limited
    status           varchar(20) NOT NULL DEFAULT 'active', -- active/expired/revoked

    starts_at        timestamptz NOT NULL DEFAULT now(),
    ends_at          timestamptz,
    reason_code      varchar(50),
    reason_details   text,

    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now(),
    revoked_at       timestamptz,
    revoked_by_admin uuid,
    revoke_reason    text
);

CREATE INDEX IF NOT EXISTS idx_sanctions_user_id ON user_sanctions (user_id);
CREATE INDEX IF NOT EXISTS idx_sanctions_status ON user_sanctions (status);
CREATE INDEX IF NOT EXISTS idx_sanctions_type ON user_sanctions (sanction_type);

------------------------------------------------------------
-- moderation_queue : file de traitement (contenus signalés)
-- On peut y pousser (via events) les flags venant ForumService/ReviewService/etc.
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS moderation_queue (
    id              bigserial PRIMARY KEY,

    source_service  varchar(50) NOT NULL, -- ForumService/ReviewService/...
    target_type     varchar(50) NOT NULL, -- topic/post/review/message
    target_id       uuid NOT NULL,

    reported_by_user_id uuid,             -- qui a signalé (Identity)
    report_reason   varchar(50),
    report_details  text,

    status          varchar(20) NOT NULL DEFAULT 'open', -- open/in_review/closed
    priority        varchar(10) NOT NULL DEFAULT 'normal', -- low/normal/high

    assigned_to_admin uuid,
    assigned_at     timestamptz,
    closed_at       timestamptz,
    close_notes     text,

    created_at      timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_mod_queue_status ON moderation_queue (status);
CREATE INDEX IF NOT EXISTS idx_mod_queue_target ON moderation_queue (source_service, target_type, target_id);

-- Evite d'avoir 20 tickets identiques pour la meme cible
CREATE UNIQUE INDEX IF NOT EXISTS uq_mod_queue_target_open
ON moderation_queue (source_service, target_type, target_id)
WHERE status IN ('open','in_review');

------------------------------------------------------------
-- admin_audit_logs : audit technique (actions admin dans backoffice)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS admin_audit_logs (
    id             bigserial PRIMARY KEY,
    admin_user_id  uuid NOT NULL,
    action         text NOT NULL,
    ip_address     inet,
    user_agent     text,
    created_at     timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_admin_audit_admin ON admin_audit_logs (admin_user_id);
CREATE INDEX IF NOT EXISTS idx_admin_audit_created ON admin_audit_logs (created_at DESC);

------------------------------------------------------------
-- outbox_messages : events sortants (Admin.ActionLogged, SanctionApplied, etc.)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS outbox_messages (
    id             bigserial PRIMARY KEY,
    message_id     uuid NOT NULL UNIQUE,
    aggregate_type varchar(100),
    aggregate_id   uuid,
    type           varchar(200) NOT NULL, -- ex: Admin.SanctionApplied
    payload        jsonb NOT NULL,
    occurred_on    timestamptz NOT NULL DEFAULT now(),
    processed_on   timestamptz,
    status         varchar(20) NOT NULL DEFAULT 'pending', -- pending/processed/failed
    error          text
);

CREATE INDEX IF NOT EXISTS idx_outbox_status ON outbox_messages (status);
CREATE INDEX IF NOT EXISTS idx_outbox_occurred_on ON outbox_messages (occurred_on);
