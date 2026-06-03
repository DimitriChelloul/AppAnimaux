CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS roles (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name text NOT NULL UNIQUE,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS users (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email text NOT NULL UNIQUE,
    password_hash text NOT NULL,
    password_algo text NOT NULL DEFAULT 'PBKDF2',
    is_email_confirmed boolean NOT NULL DEFAULT false,
    status text NOT NULL DEFAULT 'active',
    security_stamp uuid NOT NULL DEFAULT gen_random_uuid(),
    access_failed_count integer NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    last_login_at timestamptz NULL
);

CREATE TABLE IF NOT EXISTS user_roles (
    user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id uuid NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, role_id)
);

CREATE TABLE IF NOT EXISTS refresh_tokens (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash text NOT NULL UNIQUE,
    created_at timestamptz NOT NULL DEFAULT now(),
    expires_at timestamptz NOT NULL,
    revoked_at timestamptz NULL,
    revoked_reason text NULL,
    created_by_ip inet NULL,
    revoked_by_ip inet NULL,
    user_agent text NULL,
    replaced_by_token uuid NULL REFERENCES refresh_tokens(id)
);

CREATE TABLE IF NOT EXISTS login_audit (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NULL REFERENCES users(id) ON DELETE SET NULL,
    email text NOT NULL,
    succeeded boolean NOT NULL,
    reason text NOT NULL,
    ip_address inet NULL,
    user_agent text NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

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

CREATE INDEX IF NOT EXISTS ix_users_email ON users (email);
CREATE INDEX IF NOT EXISTS ix_refresh_tokens_active
    ON refresh_tokens (token_hash, expires_at)
    WHERE revoked_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_login_audit_email_created_at
    ON login_audit (email, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_outbox_messages_status_occurred_on
    ON outbox_messages (status, occurred_on);

INSERT INTO roles (name)
VALUES ('User'), ('Professional'), ('Admin')
ON CONFLICT (name) DO NOTHING;

CREATE OR REPLACE FUNCTION assign_default_user_role()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    default_role_id uuid;
BEGIN
    SELECT id INTO default_role_id
    FROM roles
    WHERE name = 'User';

    INSERT INTO user_roles (user_id, role_id)
    VALUES (NEW.id, default_role_id)
    ON CONFLICT DO NOTHING;

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_users_assign_default_role ON users;
CREATE TRIGGER trg_users_assign_default_role
AFTER INSERT ON users
FOR EACH ROW
EXECUTE FUNCTION assign_default_user_role();
