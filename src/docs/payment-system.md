# Payment System

## Objectif

`PaymentService` centralise les abonnements AppAnimaux sans mélanger les deux modèles économiques :

- abonnements utilisateur digitaux, validés côté serveur via Apple In-App Purchase ou Google Play Billing ;
- abonnements professionnels, facturés via Stripe Billing car ils référencent et promeuvent une activité physique externe à l'application.

Le front ne peut jamais activer seul un abonnement. Toute activation passe par une validation serveur, un webhook provider ou une synchronisation provider.

## Architecture

Le service suit la structure existante AppAnimaux :

- `PaymentService.Domain` : entités, enums, événements métier ;
- `PaymentService.DAL` : repositories Dapper, interfaces et UnitOfWork ;
- `PaymentService.BLL` : DTOs, validators, services applicatifs et providers Apple/Google/Stripe ;
- `PaymentService.Api` : contrôleurs, Swagger, configuration, webhooks.

La persistance utilise PostgreSQL via Dapper uniquement. Les changements métier publient des événements via l'outbox existante, consommée ensuite par RabbitMQ.

## Abonnements Utilisateur

Plans :

- `Free`
- `UserPremium`
- `UserPlus`

Ces plans donnent accès à des droits digitaux comme :

- `max_help_requests_per_month`
- `chatbot_advanced_enabled`
- `ads_disabled`
- `priority_support_enabled`

Sur iOS, le paiement passe par Apple In-App Purchase / StoreKit. Sur Android, il passe par Google Play Billing. Stripe est réservé au web si un canal web utilisateur est ajouté plus tard.

Flux mobile :

1. L'utilisateur achète via StoreKit ou Google Play Billing.
2. Le mobile envoie le reçu Apple ou le `purchaseToken` Google à `PaymentService`.
3. `PaymentService` vérifie l'achat côté provider.
4. Le reçu est stocké dans `external_purchase_receipts`.
5. `user_subscriptions` est créé ou mis à jour.
6. Les droits premium sont exposés aux autres services par API et événements RabbitMQ.

Les providers Apple/Google sont prêts pour la validation réelle. En développement, `AllowSimulatedValidation` permet de tester sans credentials Apple Developer ou Google Play Console.

## Abonnements Professionnels

Plans :

- `ProFree`
- `ProBasic`
- `ProPlus`
- `ProPremium`

Stripe Billing est utilisé car l'abonnement professionnel sert à référencer une activité externe : vétérinaire, toiletteur, pet-sitter, pension, éducateur, comportementaliste, magasin spécialisé, association ou refuge.

Flux professionnel :

1. Le professionnel choisit un plan payant.
2. L'API crée une Stripe Checkout Session.
3. Stripe confirme via `checkout.session.completed`.
4. `professional_subscriptions` est créé ou mis à jour.
5. Les webhooks `customer.subscription.*` et `invoice.*` maintiennent le statut local.
6. `ProfessionalService` peut activer, désactiver, rétrograder ou booster la fiche selon les événements et entitlements.

## Endpoints

Plans :

- `GET /api/subscription-plans/user`
- `GET /api/subscription-plans/professional`

Utilisateur :

- `GET /api/user-subscriptions/me`
- `GET /api/user-subscriptions/me/entitlements`
- `POST /api/user-subscriptions/apple/validate`
- `POST /api/user-subscriptions/google/validate`
- `POST /api/user-subscriptions/cancel`

Professionnel :

- `GET /api/professional-subscriptions/me`
- `GET /api/professional-subscriptions/me/entitlements`
- `POST /api/professional-subscriptions/create-checkout-session`
- `POST /api/professional-subscriptions/create-portal-session`
- `POST /api/professional-subscriptions/change-plan`
- `POST /api/professional-subscriptions/cancel`

Webhooks :

- `POST /api/webhooks/stripe`
- `POST /api/webhooks/apple`
- `POST /api/webhooks/google`

Admin :

- `GET /api/admin/subscriptions/users`
- `GET /api/admin/subscriptions/professionals`
- `GET /api/admin/subscriptions/{id}`
- `POST /api/admin/subscriptions/{id}/force-cancel`
- `POST /api/admin/subscriptions/{id}/sync`

## Tables

Le script `infra/postgres/init/08-payment.sql` crée :

- `subscription_plans`
- `user_subscriptions`
- `professional_subscriptions`
- `payment_provider_customers`
- `subscription_entitlements`
- `subscription_invoices`
- `external_purchase_receipts`
- `webhook_events`
- `subscription_event_logs`
- `payment_audit_logs`
- `payments`
- `outbox_messages`

Les plans et entitlements de base sont seedés dans le script.

## Webhooks Stripe

Webhooks gérés :

- `checkout.session.completed`
- `customer.subscription.created`
- `customer.subscription.updated`
- `customer.subscription.deleted`
- `invoice.paid`
- `invoice.payment_failed`

La signature Stripe est vérifiée avec `Stripe:WebhookSecret`. En développement, si le secret est vide, les webhooks sont acceptés pour faciliter les tests locaux.

## Webhooks Apple et Google

Les endpoints Apple/Google stockent et traitent les notifications serveur. La synchronisation complète des renouvellements, annulations et expirations doit être activée avec les credentials :

- Apple App Store Server API : `IssuerId`, `KeyId`, `PrivateKeyPath`, `BundleId`
- Google Play Developer API : `ServiceAccountJsonPath`, `PackageName`

## Événements RabbitMQ

Les événements sont publiés via outbox :

- `UserSubscriptionCreated`
- `UserSubscriptionRenewed`
- `UserSubscriptionCanceled`
- `UserSubscriptionExpired`
- `UserSubscriptionPaymentFailed`
- `ProfessionalSubscriptionCreated`
- `ProfessionalSubscriptionRenewed`
- `ProfessionalSubscriptionCanceled`
- `ProfessionalSubscriptionExpired`
- `ProfessionalSubscriptionPaymentFailed`
- `ProfessionalPlanChanged`
- `SubscriptionEntitlementsChanged`

Ces événements permettent à `ProfessionalService`, `UserProfileService`, `HelpRequestService`, `MessagingService`, `ChatbotService` et `AdvertisingService` d'appliquer les droits premium.

## Configuration

Sections `appsettings.json` :

- `Stripe:SecretKey`
- `Stripe:WebhookSecret`
- `Stripe:ProfessionalSuccessUrl`
- `Stripe:ProfessionalCancelUrl`
- `Stripe:CustomerPortalReturnUrl`
- `Apple:BundleId`
- `Apple:IssuerId`
- `Apple:KeyId`
- `Apple:PrivateKeyPath`
- `Apple:Environment`
- `GooglePlay:PackageName`
- `GooglePlay:ServiceAccountJsonPath`
- `Payment:DefaultCurrency`
- `Payment:TrialDays`
- `Payment:GracePeriodDays`
- `RabbitMq:HostName`
- `RabbitMq:UserName`
- `RabbitMq:Password`
- `RabbitMq:ExchangeName`

## Sécurité

Les endpoints utilisateur exigent une identité utilisateur. Les endpoints professionnels exigent `X-Professional-Id`, propagé par le gateway ou résolu ultérieurement via JWT. Les endpoints admin vérifient le rôle `Admin` via claim ou header gateway. Les webhooks Stripe vérifient la signature provider.

La prochaine amélioration naturelle est de remplacer les headers de compatibilité locale par une authentification JWT ASP.NET Core commune dans `Shared.Security`.
