# Matrice finale des événements — AppAnimaux

Audit validé sur le code, PostgreSQL réel et RabbitMQ réel.

Légende : A = flux complet ; D = contrat déclaré non produit ; E = publication inutile supprimée. La livraison est at-least-once.

## Flux actifs

| Événement | Producteur / méthode | Envelope | Outbox / transaction | Routing / exchange | Queue / binding | Consumer / handler | ACK, retry, idempotence | Statut |
|---|---|---|---|---|---|---|---|---|
| User.Registered | IdentityService / AuthService.RegisterAsync | EventEnvelope<UserRegisteredEvent> | Oui / TransactionScope + Npgsql | identity.user_registered.v1 / appanimaux.events | userprofile.events / clé exacte | UserProfileService / UserRegisteredHandler.HandleAsync | ACK après commit ; erreur vers DLQ ; Inbox + vérification du profil existant | A |
| Payment.Succeeded | PaymentService / PaymentAppService.SimulateSuccessAsync | EventEnvelope<PaymentSucceededEvent> | Oui / TransactionScope + Npgsql | payment.succeeded.v1 / appanimaux.events | subscription.events / payment.*.v1 | SubscriptionService / PaymentSucceededHandler.HandleAsync | ACK après commit ; erreur vers DLQ ; Inbox | A |
| HelpOffer.Created | HelpRequestService / HelpRequestAppService.AddOfferAsync | EventEnvelope<HelpOfferCreatedEvent> | Oui / TransactionScope + Npgsql | helprequest.offer_created.v1 / appanimaux.events | alert.events / clé exacte | AlertService / HelpRequestNotificationHandler.HandleHelpOfferCreatedAsync | ACK après commit ; erreur vers DLQ ; Inbox | A |
| HelpOffer.Accepted | HelpRequestService / HelpRequestAppService.AcceptOfferAsync | EventEnvelope<HelpOfferAcceptedEvent> | Oui / TransactionScope + Npgsql | helprequest.offer_accepted.v1 / appanimaux.events | alert.events / clé exacte | AlertService / HelpRequestNotificationHandler.HandleHelpOfferAcceptedAsync | ACK après commit ; erreur vers DLQ ; Inbox | A |
| Message.Sent | PrivateMessagingService / PrivateMessagingAppService.SendMessageAsync | EventEnvelope<MessageSentEvent> | Oui / TransactionScope + Npgsql | messaging.message_sent.v1 / appanimaux.events | alert.events / clé exacte | AlertService / HelpRequestNotificationHandler.HandleMessageSentAsync | ACK après commit ; erreur vers DLQ ; Inbox | A |

## Publications supprimées faute de besoin interservice

| Anciens événements | Ancien producteur | Décision | Statut |
|---|---|---|---|
| UserProfileService.Upserted, PhotoAdded, AvatarChanged, BannerChanged | UserProfileAppService | État local, aucun consumer | E — supprimés |
| PetService.Created, Updated, Deleted, PhotoAdded, MainPhotoChanged | PetAppService | État local, aucun consumer | E — supprimés |
| Événements ProfessionalService.* | ProfessionalAppService | Aucun consumer ou projection démontrée | E — supprimés |
| UserSubscription*, ProfessionalSubscription*, ProfessionalPlanChanged, SubscriptionEntitlementsChanged | PaymentService | Aucun consumer ; SubscriptionEventPublisher supprimé | E — supprimés |
| Subscription.Activated | PaymentSucceededHandler | Activation déjà persistée, aucun consumer | E — supprimé |
| HelpRequest.Created, HelpRequest.Published, HelpMatch.Completed | HelpRequestAppService | Aucun consumer | E — supprimés |
| Ads.ImpressionTracked, Ads.ClickTracked | AdvertisingAppService | Interactions déjà persistées, aucun pipeline analytique | E — supprimés |
| <Service>.MutationCompleted | GenericMutationOutboxMiddleware | Bruit HTTP sans sémantique métier ; middleware supprimé | E — supprimé |

Il ne reste aucun événement produit avec mapping manquant et aucun événement actif publié sans consumer : catégories B et C vides.

## Contrats déclarés mais non produits

Catégorie D :

- Payment.IntentCreated, Payment.Failed, Payment.RefundSucceeded, Payment.RefundFailed.
- Subscription.Created, Activated, Renewed, Canceled, Expired, PastDue, PlanChanged.
- Credits.WalletCreated, Granted, Reserved, Spent, ReservationCanceled, Refunded, Adjusted.
- Ads.CampaignCreated, CampaignActivated, CampaignPaused, ImpressionTracked, ClickTracked, BudgetReached.
- HelpRequest.Created, HelpRequest.Published, HelpMatch.Completed.
- Les types internes UserSubscription*, ProfessionalSubscription*, ProfessionalPlanChanged et SubscriptionEntitlementsChanged.

## Garanties techniques

### Transactional Outbox

- UseTransactionalOutbox enveloppe la requête dans un TransactionScope Required, ReadCommitted et async.
- Npgsql s’enrôle automatiquement dans la transaction ambiante.
- Test PostgreSQL réel : commit simultané métier + Outbox et rollback simultané sur exception.
- Les connexions doivent rester séquentielles. Plusieurs connexions ouvertes simultanément peuvent promouvoir la transaction en transaction préparée PostgreSQL.

### Publisher

- Lit uniquement les lignes pending éligibles.
- Utilise FOR UPDATE SKIP LOCKED, par lots de 20.
- Publie un message persistant avec publisher confirmations vers appanimaux.events.
- Marque processed uniquement après publication réussie.
- Backoff exponentiel depuis 5 secondes, plafond 1 heure, puis failed après 10 tentatives.
- RabbitMQ indisponible après commit : la ligne reste pending.
- Crash après confirmation RabbitMQ et avant commit PostgreSQL : doublon possible, donc at-least-once.

### Consumers

- Queues durables, bindings explicites, prefetch 20 et autoAck désactivé.
- Inbox et handler dans la même transaction.
- ACK après commit uniquement.
- Erreur : NACK sans requeue vers la DLQ.
- Inbox avec clé primaire message_id : redelivery idempotente.
- Aucun retry intermédiaire consumer : replay opérationnel depuis la DLQ nécessaire.

## Tests ajoutés

Le projet tests/Shared.Messaging.Tests couvre :

- DefaultEventRoutingMapper ;
- sérialisation de EventEnvelope<UserRegisteredEvent> ;
- sélection par EventHandlerRegistry ;
- commit et rollback avec PostgreSQL Testcontainers ;
- redelivery Inbox ;
- publication confirmée, binding, réception et ACK avec RabbitMQ Testcontainers.
## Renforcement production final

- Maintenance configurable toutes les 15 minutes par défaut.
- Rétention Outbox processed : 7 jours.
- Rétention Outbox failed : 30 jours.
- Rétention Inbox : 30 jours.
- Logs structurés : pending, failed, plus ancien pending, publications réussies, erreurs et purges.
- Headers propagés : message_id, event_type, correlation_id, causation_id et producer lorsqu'ils existent.
- Procédures de versioning, DLQ, replay et incidents : voir production-readiness-messaging.md.