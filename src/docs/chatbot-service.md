# ChatbotService

ChatbotService est le microservice RAG d'AppAnimaux. Il reste entierement en C#/.NET, utilise Dapper pour PostgreSQL et s'appuie sur `Shared.Semantic` pour abstraire le fournisseur LLM/embeddings.

## Pipeline

1. Validation et nettoyage de la question utilisateur.
2. Detection de signaux d'urgence animale.
3. Detection basique de prompt injection/jailbreak.
4. Chargement de la memoire de conversation recente et du resume eventuel.
5. Generation d'embedding avec cache PostgreSQL.
6. Recherche hybride pgvector + texte PostgreSQL.
7. Ranking, deduplication et limitation par budget de tokens.
8. Construction du prompt configurable.
9. Appel LLM.
10. Garde anti-hallucination simple.
11. Sauvegarde de la conversation et feedback utilisateur.

## Endpoints

- `POST /api/chatbot/ask`
- `POST /api/chatbot/document`
- `POST /api/chatbot/reindex`
- `DELETE /api/chatbot/document/{id}`
- `GET /api/chatbot/documents`
- `GET /api/chatbot/documents/statistics`
- `GET /api/chatbot/conversations/{id}`
- `POST /api/chatbot/feedback`
- `GET /api/chatbot/health`
- `GET /api/chatbot/healthz`

## Ingestion

`ITextExtractionService` extrait le texte depuis :

- texte brut, TXT et Markdown ;
- HTML avec suppression des balises et decode HTML ;
- DOCX Base64 via OpenXML ;
- PDF Base64 via PdfPig.

Les documents binaires doivent etre transmis en Base64 dans le champ `content`, avec `fileName` ou `contentType` coherent.

## Configuration

Sections principales :

- `OpenAi` : modele chat, modele embedding, base URL, temperature, cle API.
- `Embedding` : provider logique, modele, cache.
- `Chatbot` : limites conversation, chunk size/overlap et taille de message.
- `Prompt` : nom assistant, instructions systeme, taille max prompt.
- `Rag` : top K, score minimum, poids vectoriel/texte, budget contexte.

Le fournisseur actuel est OpenAI. Azure OpenAI, Mistral ou Ollama doivent etre ajoutes en implementant `IEmbeddingProvider` et `ILLMProvider`, sans modifier l'orchestrateur.

## PostgreSQL

Le schema de deploiement est dans `deploy/postgres/init/09-chatbot.sql`. Il cree :

- conversations, messages et resumes ;
- documents, chunks et embeddings ;
- cache d'embeddings ;
- feedback utilisateur ;
- index pgvector et index texte PostgreSQL.

## Observabilite

Le service expose des logs structures et des metriques OpenTelemetry via le meter `AppAnimaux.ChatbotService` :

- `chatbot.requests` ;
- `chatbot.embeddings` ;
- `chatbot.vector_searches` ;
- `chatbot.llm_calls` ;
- `chatbot.response_time_ms` ;
- `chatbot.retrieved_chunks`.

L'export OTLP est active via `OpenTelemetry.Exporter.OpenTelemetryProtocol`. Un collector OpenTelemetry peut ensuite exposer ces metriques vers Prometheus.

## Notes packages

`Microsoft.AspNetCore.OpenApi` est mis a jour en `10.0.9`. Le generateur ASP.NET Core 10 reste compatible avec la branche `Microsoft.OpenApi` 2.x ; la version 3.x casse le source generator. Le projet force donc `Microsoft.OpenApi` 2.3.12 et contient une suppression d'audit ciblee pour l'advisory upstream jusqu'a compatibilite 3.x.
