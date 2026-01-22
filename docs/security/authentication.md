# Authentication and API Keys

## API key lifecycle
1) **Generation**: `ApiKeyService.CreateAsync` creates a random 32-byte token and returns a one-time plaintext key.
2) **Display**: plaintext is returned only once on creation; it is never stored.
3) **Storage**: PBKDF2 hash (SHA-256) + per-key salt is stored alongside key metadata.
4) **Verification**: `ApiKeyAuthMiddleware` uses `ApiKeyService.ValidateAsync`, which verifies with constant-time comparison and checks revoke/expiry.

API keys are formatted as `hub_<token>`. A prefix derived from the token is stored to avoid full-table scans.

## PBKDF2 parameters and secrets
Configured in `SecurityOptions` (loaded from `Security` configuration section):
- `ApiKeyPepper` (required): appended to the plaintext before hashing.
- `DefaultIterations`: default 210,000.
- `SaltBytes`: default 16.
- `HashBytes`: default 32.
- `PrefixLength`: default 10.

If `Security:ApiKeyPepper` is missing, the API throws during startup.

## Scopes and enforcement
`ApiKeyScopes` is a `[Flags]` enum: `Read`, `Ingest`, `Admin`.
- `RequireScopes` endpoint filter checks `(auth.Scopes & required) == required`.
- Admin routes are grouped under `/admin` and require `Admin` scope.

## Bootstrap endpoint (dev only)
`POST /admin/bootstrap` is only mapped in Development environments.
- Requires `X-Bootstrap-Token` header matching `Security:BootstrapToken`.
- Creates a default workspace and returns a one-time admin API key.
- Intended only for local development; do not enable or expose in production.

## Example
```bash
curl -s -X POST http://localhost:5119/admin/bootstrap \
  -H "X-Bootstrap-Token: BOOTSTRAP_DEV_TOKEN" | jq .
```
