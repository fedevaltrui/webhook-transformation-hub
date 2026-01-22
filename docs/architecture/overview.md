# Architecture Overview

## High-level architecture
- **Hub.Api** hosts Minimal API endpoints, auth middleware, and request capture.
- **Hub.Infrastructure** provides EF Core, PostgreSQL mappings, and security services.
- **Hub.Domain** defines entities and shared enums.
- **PostgreSQL** stores workspaces, API keys, endpoints, ingests, and deliveries.
- **DeliveryWorker** runs in the API host as a background service to deliver pending webhooks.
- **Redis** is provisioned for future delivery/worker processing.

## Request flow diagrams
### Admin request (API key required)
```
Client
  |  X-Api-Key
  v
ApiKeyAuthMiddleware
  |  validates key, sets RequestAuthContext
  v
RequireScopes filter (Admin)
  |  401/403 if missing or insufficient scopes
  v
Admin endpoint handler
  |  EF Core writes/reads
  v
PostgreSQL
```

### Public ingest request
```
Client
  |  POST /ingest/{endpointKey}
  v
Ingest handler
  |  resolves endpointKey
  |  idempotency check
  |  captures headers/body
  v
PostgreSQL
  |  IngestRequest + Delivery(Pending)
  v
202 Accepted (requestId)
```

### Delivery processing
```
DeliveryWorker
  |  polls Deliveries where Status=Pending and NextAttemptAtUtc <= now
  v
POST to Endpoint.DestinationUrl
  |  Success -> Status=Success
  |  Non-2xx/exception -> Status=Pending + NextAttemptAtUtc (backoff)
  v
PostgreSQL
```

## Key components
- `ApiKeyAuthMiddleware`: Extracts `X-Api-Key`, validates with `ApiKeyService`, populates `RequestAuthContext`, and enriches Serilog log context.
- `RequestAuthContext`: Scoped auth state containing workspace, API key ID, and scopes for the current request.
- `RequireScopes` filter: Enforces required flags on route groups; returns 401/403 when missing.
- `DeliveryWorker`: Background service that attempts deliveries, records status/attempts, and schedules retries.

## Persistence and why it happens
- **IngestRequest** captures headers and body as JSON to ensure webhook payloads are retained even before any worker is available.
- **Delivery** is created immediately with `Pending` status and updated by `DeliveryWorker` as attempts progress.
- **Workspace isolation** is enforced by using the authenticated workspace ID in admin queries.

## Example
```bash
curl -s -X GET http://localhost:5119/health | jq .
```
