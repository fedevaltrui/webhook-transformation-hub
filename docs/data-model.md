# Data Model

## Entities
### Workspace
- `Id` (GUID)
- `Name` (max 120)
- `CreatedAtUtc`

### ApiKey
- `Id`, `WorkspaceId`, `Name`
- `KeyPrefix` (prefix lookup)
- `KeyHash` (PBKDF2 hash, base64)
- `KeySalt` (base64)
- `KeyIterations`
- `Scopes` (`ApiKeyScopes` flags, stored as int)
- `CreatedAtUtc`, `ExpiresAtUtc`, `RevokedAtUtc`, `LastUsedAtUtc`

### Endpoint
- `Id`, `WorkspaceId`, `Name`
- `EndpointKey` (public key used in `/ingest/{endpointKey}`)
- `DestinationUrl`
- `SigningSecret` (reserved for future HMAC)
- `IsActive`, `CreatedAtUtc`

### IngestRequest
- `Id`, `EndpointId`
- `ReceivedAtUtc`, `Method`
- `HeadersJson` (jsonb)
- `BodyJson` (jsonb)
- `IdempotencyKey`

### Delivery
- `Id`, `IngestRequestId`
- `Attempt`, `Status`
- `CreatedAtUtc`, `StartedAtUtc`, `FinishedAtUtc`
- `NextAttemptAtUtc`
- `ResponseStatusCode`, `Error`
  - Status values: `Pending`, `InProgress`, `Success`, `Failed`

## Relationships
- `Workspace` 1—* `ApiKey`
- `Workspace` 1—* `Endpoint`
- `Endpoint` 1—* `IngestRequest`
- `IngestRequest` 1—* `Delivery`

## Indexes and constraints
- `ApiKey`: unique index on `KeyHash`, index on `KeyPrefix`
- `Endpoint`: unique index on `EndpointKey`
- `IngestRequest`: composite index on `(EndpointId, IdempotencyKey)`
- `Delivery`: composite index on `(IngestRequestId, Attempt)`
- `Workspace`: index on `Name`

## jsonb usage
`HeadersJson` and `BodyJson` are stored as PostgreSQL `jsonb`. Non-JSON payloads are captured as base64 with a content-type wrapper.

## Example
```sql
SELECT id, name FROM "Workspaces" ORDER BY "CreatedAtUtc" DESC;
```
