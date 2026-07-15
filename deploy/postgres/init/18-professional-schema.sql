-- 18-professional-schema.sql
-- Schema du ProfessionalService (professional_db)
connect professional_db

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "citext";

CREATE TABLE IF NOT EXISTS professionals (
    id                    uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id               uuid NOT NULL UNIQUE,
    business_name         text NOT NULL,
    category              varchar(80) NOT NULL,
    description           text,
    address               text,
    city                  text,
    postal_code           varchar(20),
    latitude              double precision,
    longitude             double precision,
    phone                 varchar(40),
    email                 citext,
    website               text,
    subscription_plan     varchar(40) NOT NULL DEFAULT 'none',
    subscription_status   varchar(30) NOT NULL DEFAULT 'inactive',
    is_verified           boolean NOT NULL DEFAULT false,
    average_rating        numeric(3,2) NOT NULL DEFAULT 0,
    review_count          int NOT NULL DEFAULT 0,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT ck_professionals_subscription_plan
        CHECK (subscription_plan IN ('none', 'professional_basic', 'professional_premium', 'professional_premium_plus')),
    CONSTRAINT ck_professionals_subscription_status
        CHECK (subscription_status IN ('inactive', 'trialing', 'active', 'past_due', 'canceled'))
);

CREATE INDEX IF NOT EXISTS idx_professionals_category ON professionals (category);
CREATE INDEX IF NOT EXISTS idx_professionals_city ON professionals (city);
CREATE INDEX IF NOT EXISTS idx_professionals_subscription ON professionals (subscription_status, subscription_plan);
CREATE INDEX IF NOT EXISTS idx_professionals_geo ON professionals (latitude, longitude);

CREATE TABLE IF NOT EXISTS professional_services (
    id                uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    professional_id   uuid NOT NULL,
    service_name      text NOT NULL,
    description       text,
    price_range       text,
    display_order     int NOT NULL DEFAULT 0,
    created_at        timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_professional_services_professional
        FOREIGN KEY (professional_id) REFERENCES professionals(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_professional_services_professional_id ON professional_services (professional_id);

CREATE TABLE IF NOT EXISTS professional_photos (
    id                uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    professional_id   uuid NOT NULL,
    media_id          uuid NOT NULL,
    media_url         text,
    display_order     int NOT NULL DEFAULT 0,
    caption           text,
    is_primary        boolean NOT NULL DEFAULT false,
    created_at        timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_professional_photos_professional
        FOREIGN KEY (professional_id) REFERENCES professionals(id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_professional_photos_media ON professional_photos (professional_id, media_id);
CREATE INDEX IF NOT EXISTS idx_professional_photos_professional_id ON professional_photos (professional_id);

CREATE TABLE IF NOT EXISTS professional_opening_hours (
    id                uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    professional_id   uuid NOT NULL,
    day_of_week       smallint NOT NULL CHECK (day_of_week BETWEEN 1 AND 7),
    opens_at          time,
    closes_at         time,
    is_closed         boolean NOT NULL DEFAULT false,

    CONSTRAINT fk_professional_hours_professional
        FOREIGN KEY (professional_id) REFERENCES professionals(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_professional_hours_professional_id ON professional_opening_hours (professional_id);

CREATE TABLE IF NOT EXISTS outbox_messages (
    id             bigserial PRIMARY KEY,
    message_id     uuid NOT NULL UNIQUE,
    aggregate_type varchar(100),
    aggregate_id   uuid,
    type           varchar(200) NOT NULL,
    payload        jsonb NOT NULL,
    occurred_on    timestamptz NOT NULL DEFAULT now(),
    processed_on   timestamptz,
    status         varchar(20) NOT NULL DEFAULT 'pending',
    error          text,
    attempts       integer NOT NULL DEFAULT 0,
    next_attempt_on timestamptz
);

CREATE INDEX IF NOT EXISTS idx_professional_outbox_status_attempt
    ON outbox_messages (status, next_attempt_on, occurred_on);
