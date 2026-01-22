# Webhook Transformation Hub

## What is this?
Webhook Transformation Hub is a lightweight ingestion and routing service for multi-tenant webhook capture. It provides a public ingest endpoint per workspace and keeps a durable record of every request for delivery, inspection, and transformation.

The project is built on .NET 10 Minimal APIs with PostgreSQL for persistence and Redis (reserved for worker usage). The current milestone focuses on API key security, endpoint registry, and ingest capture.

## Key features
- Multi-tenant API key authentication with scopes and workspace isolation
- One-time API key generation with PBKDF2 + per-key salt + global pepper
- Admin endpoints for workspaces, API keys, and endpoint registry
- Public ingest endpoint with idempotency and request capture (headers/body)
- Background delivery worker with retries and backoff
- Basic health endpoint and local dev bootstrap flow

## Quickstart
1) Start dependencies:
```bash
docker compose -f docker/docker-compose.yml up -d
```
PostgreSQL listens on `localhost:5433` and Redis on `localhost:6379`.

2) Restore tools and apply migrations:
```bash
dotnet tool restore
dotnet ef database update \
  --project src/Hub.Infrastructure/Hub.Infrastructure.csproj \
  --startup-project src/Hub.Api/Hub.Api.csproj
```

3) Run the API:
```bash
dotnet run --project src/Hub.Api/Hub.Api.csproj
```

Default dev URL (from `launchSettings.json`): `http://localhost:5119`.

## Configuration
### Security
- `Security:ApiKeyPepper` (required): global secret used in PBKDF2 derivation.
- `Security:BootstrapToken` (development only): token for `POST /admin/bootstrap`.
- Development defaults are in `src/Hub.Api/appsettings.Development.json` and should be replaced locally.
### Database
- `ConnectionStrings:Postgres` must point to the running Postgres instance.

### Delivery
- `Delivery` records are created at ingest time and processed by `DeliveryWorker`.
- Delivery retries use exponential backoff with configurable limits.
- Configuration (defaults shown in `appsettings.Development.json`):
  - `Delivery:PollSeconds`
  - `Delivery:MaxAttempts`
  - `Delivery:BaseDelaySeconds`
  - `Delivery:MaxDelaySeconds`
  - `Delivery:HttpTimeoutSeconds`
  - `Delivery:BatchSize`
- Redis is provisioned in `docker/docker-compose.yml` for future usage.

## API overview
- `docs/api/endpoints.md` for the complete HTTP surface and examples.
- `docs/security/authentication.md` for API key lifecycle and auth details.
- `docs/architecture/overview.md` for request flow and components.
- `docs/data-model.md` for entities, relations, and indexes.
- `docs/decisions/ADR-0001-api-keys.md` for the API key design decision.
- Development OpenAPI/Swagger: `/openapi/v1.json` and `/swagger` (Development only).

## What we built so far
- Issue #5: Multi-tenant API key auth with PBKDF2+salt+pepper, scoped admin enforcement, and dev-only bootstrap flow.
- Issue #6/A1: Endpoint registry, ingest pipeline v1 with idempotency, and admin inspection endpoints for ingests and deliveries.

## Demo script
```bash
BASE_URL=http://localhost:5119
BOOTSTRAP_TOKEN=BOOTSTRAP_DEV_TOKEN

# 1) Bootstrap (dev only) -> workspace + admin API key
ADMIN_KEY=$(curl -s -X POST "$BASE_URL/admin/bootstrap" \
  -H "X-Bootstrap-Token: $BOOTSTRAP_TOKEN" \
  | jq -r .apiKey)

# 2) Create an endpoint
ENDPOINT_ID=$(curl -s -X POST "$BASE_URL/admin/endpoints" \
  -H "X-Api-Key: $ADMIN_KEY" \
  -H "Content-Type: application/json" \
  -d '{"name":"Orders","destinationUrl":"https://example.com/webhooks/orders","isActive":true}' \
  | jq -r .id)

# 3) List endpoints to retrieve endpointKey
ENDPOINT_KEY=$(curl -s -X GET "$BASE_URL/admin/endpoints" \
  -H "X-Api-Key: $ADMIN_KEY" \
  | jq -r '.[0].endpointKey')

# 4) Ingest a webhook
INGEST_ID=$(curl -s -X POST "$BASE_URL/ingest/$ENDPOINT_KEY" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: demo-001" \
  -d '{"orderId":123,"status":"created"}' \
  | jq -r .requestId)

# 5) List ingests for the endpoint
curl -s -X GET "$BASE_URL/admin/endpoints/$ENDPOINT_ID/ingests?limit=10" \
  -H "X-Api-Key: $ADMIN_KEY" | jq .

# 6) Inspect a single ingest
curl -s -X GET "$BASE_URL/admin/ingests/$INGEST_ID" \
  -H "X-Api-Key: $ADMIN_KEY" | jq .
```
