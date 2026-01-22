# API Endpoints

Base URL (development): `http://localhost:5119`

All admin endpoints require `X-Api-Key` with `Admin` scope. The public ingest endpoint does not require auth.

## GET /openapi/v1.json (Development only)
- **Auth**: none
- **Headers**: none
- **Request body**: none
- **Response 200**: OpenAPI document (JSON)

## GET /swagger (Development only)
- **Auth**: none
- **Headers**: none
- **Request body**: none
- **Response 200**: Swagger UI

## POST /admin/bootstrap (Development only)
- **Auth**: none
- **Headers**: `X-Bootstrap-Token: <token>`
- **Request body**: none
- **Response 200**:
```json
{
  "workspaceId": "<guid>",
  "apiKeyId": "<guid>",
  "apiKey": "hub_<token>",
  "scopes": "Admin, Read"
}
```
- **Errors**:
  - `401` when the bootstrap token is missing or invalid

## POST /admin/workspaces
- **Auth**: API key with `Admin` scope
- **Headers**: `X-Api-Key: hub_<token>`, `Content-Type: application/json`
- **Request body**:
```json
{
  "name": "Acme"
}
```
- **Response 200**:
```json
{
  "id": "<guid>",
  "name": "Acme",
  "createdAtUtc": "2025-01-01T00:00:00+00:00"
}
```
- **Errors**:
  - `401` when the API key is missing/invalid
  - `403` when the key lacks `Admin`

## POST /admin/apikeys
- **Auth**: API key with `Admin` scope
- **Headers**: `X-Api-Key: hub_<token>`, `Content-Type: application/json`
- **Request body**:
```json
{
  "workspaceId": "<guid>",
  "name": "ci-admin",
  "scopes": "Admin",
  "expiresAtUtc": null
}
```
- **Response 200**:
```json
{
  "apiKeyId": "<guid>",
  "apiKey": "hub_<token>",
  "workspaceId": "<guid>",
  "scopes": "Admin",
  "expiresAtUtc": null
}
```
- **Errors**:
  - `401` when the API key is missing/invalid
  - `403` when the key lacks `Admin`

## POST /admin/apikeys/{id}/revoke
- **Auth**: API key with `Admin` scope
- **Headers**: `X-Api-Key: hub_<token>`
- **Request body**: none
- **Response 200**:
```json
{ "revoked": true }
```
- **Errors**:
  - `401` when the API key is missing/invalid
  - `403` when the key lacks `Admin`
  - `404` when the API key ID does not exist

## POST /admin/endpoints
- **Auth**: API key with `Admin` scope
- **Headers**: `X-Api-Key: hub_<token>`, `Content-Type: application/json`
- **Request body**:
```json
{
  "name": "Orders",
  "destinationUrl": "https://example.com/webhooks/orders",
  "isActive": true,
  "signingSecret": null
}
```
- **Response 200**:
```json
{
  "id": "<guid>",
  "name": "Orders",
  "endpointKey": "<public-key>",
  "destinationUrl": "https://example.com/webhooks/orders",
  "isActive": true,
  "createdAtUtc": "2025-01-01T00:00:00+00:00"
}
```
- **Errors**:
  - `400` when `name` is blank or `destinationUrl` is not a valid absolute URL
  - `401` when the API key is missing/invalid
  - `403` when the key lacks `Admin`

## GET /admin/endpoints
- **Auth**: API key with `Admin` scope
- **Headers**: `X-Api-Key: hub_<token>`
- **Request body**: none
- **Response 200**:
```json
[
  {
    "id": "<guid>",
    "name": "Orders",
    "endpointKey": "<public-key>",
    "destinationUrl": "https://example.com/webhooks/orders",
    "isActive": true,
    "createdAtUtc": "2025-01-01T00:00:00+00:00"
  }
]
```
- **Errors**:
  - `401` when the API key is missing/invalid
  - `403` when the key lacks `Admin`

## POST /ingest/{endpointKey}
- **Auth**: none
- **Headers**: `Idempotency-Key` (optional), `Content-Type`
- **Request body**: any JSON; non-JSON payloads are captured as base64
- **Response 202**:
```json
{ "requestId": "<guid>", "duplicated": false }
```
- **Response 202 (idempotent hit)**:
```json
{ "requestId": "<guid>", "duplicated": true }
```
- **Errors**:
  - `404` when the endpoint key is not found
  - `403` when the endpoint is inactive

## GET /admin/endpoints/{endpointId}/ingests
- **Auth**: API key with `Admin` scope
- **Headers**: `X-Api-Key: hub_<token>`
- **Query params**: `limit` (1-200, default 50)
- **Response 200**:
```json
{
  "endpointId": "<guid>",
  "count": 1,
  "items": [
    {
      "id": "<guid>",
      "receivedAtUtc": "2025-01-01T00:00:00+00:00",
      "method": "POST",
      "idempotencyKey": "demo-001",
      "lastDelivery": {
        "status": "Pending",
        "attempt": 1,
        "responseStatusCode": null,
        "error": null,
        "startedAtUtc": null
      }
    }
  ]
}
```
- **Errors**:
  - `401` when the API key is missing/invalid
  - `403` when the key lacks `Admin`
  - `404` when the endpoint does not belong to the workspace

## GET /admin/ingests/{ingestId}
- **Auth**: API key with `Admin` scope
- **Headers**: `X-Api-Key: hub_<token>`
- **Request body**: none
- **Response 200**:
```json
{
  "id": "<guid>",
  "endpointId": "<guid>",
  "endpointName": "Orders",
  "receivedAtUtc": "2025-01-01T00:00:00+00:00",
  "method": "POST",
  "idempotencyKey": "demo-001",
  "headers": { "Content-Type": "application/json" },
  "body": { "orderId": 123 },
  "deliveries": [
    {
      "id": "<guid>",
      "attempt": 1,
      "status": "Pending",
      "responseStatusCode": null,
      "error": null,
      "startedAtUtc": null
    }
  ]
}
```
- **Errors**:
  - `401` when the API key is missing/invalid
  - `403` when the key lacks `Admin`
  - `404` when the ingest does not belong to the workspace

## GET /health
- **Auth**: none
- **Headers**: none
- **Request body**: none
- **Response 200**:
```json
{ "status": "ok", "service": "webhook-transformation-hub", "utc": "2025-01-01T00:00:00+00:00" }
```

## GET /db-ping
- **Auth**: none
- **Headers**: none
- **Request body**: none
- **Response 200**:
```json
{ "postgres": true }
```

## Examples
```bash
curl -s -X GET http://localhost:5119/health | jq .
```
