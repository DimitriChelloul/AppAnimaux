-- 01-identity-schema.sql
-- Schéma de la base identity_db (service IdentityService)
connect identity_db

-- Si tu es dans psql :
-- \connect identity_db;

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "citext";

------------------------------------------------------------
-- Table users : comptes utilisateurs (auth)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS users (
    id                  uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    email               citext NOT NULL UNIQUE,
    password_hash       text   NOT NULL,
    password_algo       varchar(50) NOT NULL DEFAULT 'PBKDF2',
    is_email_confirmed  boolean NOT NULL DEFAULT false,
    email_confirmed_at  timestamptz,
    phone_number        varchar(32),
    is_phone_confirmed  boolean NOT NULL DEFAULT false,
    two_factor_enabled  boolean NOT NULL DEFAULT false,
    security_stamp      uuid NOT NULL DEFAULT uuid_generate_v4(),
    lockout_end         timestamptz,
    access_failed_count int NOT NULL DEFAULT 0,
    status              varchar(20) NOT NULL DEFAULT 'active', -- active / disabled / blocked
    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now(),
    last_login_at       timestamptz
);

-- Index utile pour les recherches par email
CREATE INDEX IF NOT EXISTS idx_users_email ON users (email);

------------------------------------------------------------
-- Table roles : rôles applicatifs (admin, user, modérateur, etc.)
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS roles (
    id              smallserial PRIMARY KEY,
    name            varchar(50) NOT NULL UNIQUE,
    normalized_name varchar(50) NOT NULL UNIQUE,
    description     text,
    created_at      timestamptz NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- Table user_roles : association N-N entre users et roles
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS user_roles (
    user_id uuid       NOT NULL,
    role_id smallint   NOT NULL,
    assigned_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, role_id),
    CONSTRAINT fk_user_roles_user
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_user_roles_role
        FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE CASCADE
);

-- Index pour retrouver tous les users d’un rôle rapidement
CREATE INDEX IF NOT EXISTS idx_user_roles_role_id ON user_roles (role_id);

------------------------------------------------------------
-- Table refresh_tokens : gestion des refresh tokens JWT
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS refresh_tokens (
    id                uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id           uuid NOT NULL,
    token_hash        text NOT NULL,
    created_at        timestamptz NOT NULL DEFAULT now(),
    expires_at        timestamptz NOT NULL,
    revoked_at        timestamptz,
    revoked_reason    text,
    replaced_by_token uuid,
    created_by_ip     inet,
    revoked_by_ip     inet,
    user_agent        text,
    CONSTRAINT fk_refresh_tokens_user
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user_id ON refresh_tokens (user_id);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_expires_at ON refresh_tokens (expires_at);

------------------------------------------------------------
-- Table login_audit : trace des connexions / tentatives
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS login_audit (
    id          bigserial PRIMARY KEY,
    user_id     uuid,
    email       citext,
    succeeded   boolean NOT NULL,
    reason      varchar(50), -- success / bad_password / user_not_found / locked_out / etc.
    ip_address  inet,
    user_agent  text,
    created_at  timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_login_audit_user
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_login_audit_user_id ON login_audit (user_id);
CREATE INDEX IF NOT EXISTS idx_login_audit_created_at ON login_audit (created_at);

------------------------------------------------------------
-- Table outbox_messages : pattern outbox pour publier des events
------------------------------------------------------------
CREATE TABLE IF NOT EXISTS outbox_messages (
    id             bigserial PRIMARY KEY,
    message_id     uuid NOT NULL UNIQUE,
    aggregate_type varchar(100),
    aggregate_id   uuid,
    type           varchar(200) NOT NULL,  -- ex : Identity.UserRegistered
    payload        jsonb        NOT NULL,
    occurred_on    timestamptz  NOT NULL DEFAULT now(),
    processed_on   timestamptz,
    status         varchar(20)  NOT NULL DEFAULT 'pending', -- pending / processed / failed
    error          text,
    attempts       integer NOT NULL DEFAULT 0,
    next_attempt_on timestamptz
);

CREATE INDEX IF NOT EXISTS idx_outbox_status ON outbox_messages (status);
CREATE INDEX IF NOT EXISTS idx_outbox_occurred_on ON outbox_messages (occurred_on);
