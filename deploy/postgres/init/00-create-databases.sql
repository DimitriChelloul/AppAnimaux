-- 00-create-databases.sql
-- Création du rôle applicatif + bases par microservice AppAnimaux

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'app_user') THEN
    CREATE ROLE app_user WITH LOGIN PASSWORD 'ChangeMe!';
  END IF;
END$$;

-- IdentityService
CREATE DATABASE identity_db OWNER app_user;
GRANT ALL PRIVILEGES ON DATABASE identity_db TO app_user;

-- UserProfileService
CREATE DATABASE userprofile_db OWNER app_user;
GRANT ALL PRIVILEGES ON DATABASE userprofile_db TO app_user;

-- PetService
CREATE DATABASE pet_db OWNER app_user;
GRANT ALL PRIVILEGES ON DATABASE pet_db TO app_user;

-- HelpRequestService
CREATE DATABASE helprequest_db OWNER app_user;
GRANT ALL PRIVILEGES ON DATABASE helprequest_db TO app_user;

-- MediaService
CREATE DATABASE media_db OWNER app_user;
GRANT ALL PRIVILEGES ON DATABASE media_db TO app_user;

-- NotificationService
CREATE DATABASE notification_db OWNER app_user;
GRANT ALL PRIVILEGES ON DATABASE notification_db TO app_user;

-- ReviewService
CREATE DATABASE review_db OWNER app_user;
GRANT ALL PRIVILEGES ON DATABASE review_db TO app_user;

-- ForumService
CREATE DATABASE forum_db OWNER app_user;
GRANT ALL PRIVILEGES ON DATABASE forum_db TO app_user;

-- PrivateMessagingService
CREATE DATABASE privatemessaging_db OWNER app_user;
GRANT ALL PRIVILEGES ON DATABASE privatemessaging_db TO app_user;

-- ChatbotService
CREATE DATABASE chatbot_db OWNER app_user;
GRANT ALL PRIVILEGES ON DATABASE chatbot_db TO app_user;

-- AdminService
CREATE DATABASE admin_db OWNER app_user;
GRANT ALL PRIVILEGES ON DATABASE admin_db TO app_user;

-- ReportingService
CREATE DATABASE reporting_db OWNER app_user;
GRANT ALL PRIVILEGES ON DATABASE reporting_db TO app_user;

-- PaymentService
CREATE DATABASE payment_db OWNER app_user;
GRANT ALL PRIVILEGES ON DATABASE payment_db TO app_user;

-- LocationService
CREATE DATABASE location_db OWNER app_user;
GRANT ALL PRIVILEGES ON DATABASE location_db TO app_user;

-- SubscriptionService
CREATE DATABASE subscription_db OWNER app_user;
GRANT ALL PRIVILEGES ON DATABASE subscription_db TO app_user;

-- CreditService (Tokens)
CREATE DATABASE credit_db OWNER app_user;
GRANT ALL PRIVILEGES ON DATABASE credit_db TO app_user;

-- AdvertisingService
CREATE DATABASE advertising_db OWNER app_user;
GRANT ALL PRIVILEGES ON DATABASE advertising_db TO app_user;

-- AlertService
CREATE DATABASE alert_db OWNER app_user;
GRANT ALL PRIVILEGES ON DATABASE alert_db TO app_user;
