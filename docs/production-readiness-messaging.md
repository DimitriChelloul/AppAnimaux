# Production readiness — Shared.Messaging

## Événements actifs et justification

| Événement v1 | Justification métier | Producteur | Consommateur | Routing key |
|---|---|---|---|---|
| `User.Registered` | Créer automatiquement le profil associé à une nouvelle identité | IdentityService | UserProfileService | `identity.user_registered.v1` |
| `Payment.Succeeded` | Activer l'abonnement correspondant à un paiement confirmé | PaymentService | SubscriptionService | `payment.succeeded.v1` |
| `HelpOffer.Created` | Notifier le demandeur d'une nouvelle proposition d'aide | HelpRequestService | AlertService | `helprequest.offer_created.v1` |
| `HelpOffer.Accepted` | Notifier l'aidant que sa proposition est acceptée | HelpRequestService | AlertService | `helprequest.offer_accepted.v1` |
| `Message.Sent` | Notifier les destinataires d'un nouveau message privé | PrivateMessagingService | AlertService | `messaging.message_sent.v1` |

Chaque contrat utilise `EventEnvelope<T>` version 1. La livraison est at-least-once et les consumers utilisent `inbox_messages.message_id` pour neutraliser les doublons.

## Règle de versioning

Une version existante est immuable du point de vue des consumers :

- un champ peut être ajouté uniquement s'il est optionnel ou possède une valeur par défaut compatible ;
- un champ ne doit pas être supprimé, renommé, changer de type ou changer de sens dans la même version ;
- tout changement cassant crée un nouveau type/version et une routing key `.v2` ;
- v1 et v2 doivent coexister pendant la migration ; le consumer ajoute d'abord le binding et le handler v2, puis le producteur commence à publier v2 ;
- le binding v1 n'est retiré qu'après drainage des queues et expiration de la durée maximale de rétention/replay ;
- les tests de désérialisation des anciens payloads sont conservés tant que v1 est supportée.

## Métadonnées et traçabilité

`IntegrationEvent` porte `EventId`, `OccurredOn`, `CorrelationId`, `CausationId` et `SourceService`. `EventEnvelope` porte le `MessageId`, le type, la version et le payload.

Le publisher conserve le même `MessageId` à chaque tentative et propage les headers suivants lorsqu'ils existent :

- `message_id` et `event_type` ;
- `correlation_id` et `causation_id` ;
- `producer`.

Les logs de publication n'incluent ni payload, ni email, ni contenu de message.

## Outbox et observabilité

Le publisher sélectionne les lignes `pending` éligibles avec `FOR UPDATE SKIP LOCKED`. Une transaction PostgreSQL garde les lignes verrouillées pendant la publication confirmée et le changement de statut. Deux instances ne sélectionnent donc pas le même lot pendant le polling normal.

La maintenance journalise périodiquement :

- le nombre de lignes `pending` ;
- le nombre de lignes `failed` ;
- la date du plus ancien message `pending` ;
- le nombre de lignes Outbox et Inbox purgées.

Valeurs par défaut configurables dans `RabbitMq` :

- `MaintenanceIntervalMinutes`: 15 ;
- `ProcessedOutboxRetentionDays`: 7 ;
- `FailedOutboxRetentionDays`: 30 ;
- `InboxRetentionDays`: 30.

Un passage en `failed` génère un log critique. Les lignes `failed` restent disponibles 30 jours pour diagnostic et replay.

## DLQ et replay contrôlé

Chaque consumer possède :

- exchange DLQ : `<exchange>.dead-letter`, donc `appanimaux.events.dead-letter` ;
- queue DLQ : `<queue>.dead-letter`, par exemple `userprofile.events.dead-letter` ;
- binding DLQ : `#`.

Procédure :

1. Inspecter la DLQ depuis l'interface RabbitMQ sans acquitter massivement les messages.
2. Relever `message_id`, `event_type`, routing key d'origine, headers et erreur du consumer.
3. Corriger la cause et déployer le consumer.
4. Republier vers `appanimaux.events` avec la routing key d'origine, le corps inchangé et le `MessageId` original.
5. Vérifier l'ACK et l'entrée Inbox. Si le message avait déjà produit un effet, l'Inbox empêche sa répétition.
6. Acquitter/supprimer l'original de la DLQ seulement après validation.

Ne jamais générer un nouveau `MessageId` lors d'un replay.

## Procédures d'incident

### RabbitMQ indisponible

- Ne pas annuler les transactions métier déjà committées.
- Vérifier que l'Outbox reste `pending` et que `attempts` augmente avec backoff.
- Restaurer RabbitMQ ; le publisher reprend automatiquement.
- Surveiller l'âge du plus ancien `pending` jusqu'au retour à zéro.

### Outbox bloquée

- Contrôler `status`, `attempts`, `next_attempt_on`, `error`, type et âge.
- Vérifier le mapping, la connectivité RabbitMQ et l'existence d'une queue liée.
- Ne pas modifier le payload ni le `message_id`.
- Pour une ligne corrigée en `failed`, la remettre explicitement à `pending`, vider `error`, et fixer `next_attempt_on = now()` après validation opérateur.

### Message failed

- Le log critique et le compteur périodique doivent déclencher une alerte.
- Diagnostiquer avant toute remise à `pending`.
- Les erreurs de contrat permanentes ne doivent pas être retentées sans correction/déploiement.

### Doublon

- Rechercher le `message_id` dans `inbox_messages`.
- Un doublon après crash publisher est attendu dans le modèle at-least-once.
- Ne supprimer une entrée Inbox que dans le cadre de la politique de rétention, jamais pour forcer un replay récent.

## Shutdown

Le publisher reçoit le token d'arrêt dans la publication et la transaction : un message non terminé n'est pas marqué `processed`. Le consumer annule son abonnement puis ferme le channel ; tout message sans ACK est redélivré par RabbitMQ et neutralisé par l'Inbox si son traitement avait déjà été committé.

## Risques assumés

- Un crash après confirmation RabbitMQ mais avant commit du statut Outbox produit un doublon possible.
- Il n'y a pas de retry consumer automatique : toute erreur va en DLQ afin d'éviter de répéter aveuglément une erreur métier permanente.
- Les logs structurés sont disponibles, mais leur transformation en alertes dépend de la plateforme d'observabilité du déploiement.
- Les connexions Npgsql au sein d'un `TransactionScope` doivent rester séquentielles ; plusieurs connexions simultanées peuvent promouvoir la transaction vers les transactions préparées PostgreSQL.

## Couverture de résilience

- Deux publishers concurrents : une seule sélection/publication pendant le polling normal grâce à SKIP LOCKED.
- Crash après publication avant marquage : doublon assumé et neutralisé par la clé Inbox.
- Deux redeliveries : une seule insertion Inbox et un seul effet métier.
- Arrêt avant ACK : RabbitMQ redélivre ; le MessageId stable permet l'idempotence.
- Message malformé : NACK sans requeue puis DLQ, sans blocage de la queue.
- Événement inconnu : rejet explicite puis DLQ, sans boucle infinie.