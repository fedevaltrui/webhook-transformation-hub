# ADR-0001: API Keys with PBKDF2 + Salt + Pepper and Scoped Flags

## Status
Accepted

## Context
The service needs a simple, secure authentication model for a multi-tenant API with admin-only endpoints. Keys must be validated quickly, support rotation/revocation, and avoid storing plaintext secrets.

## Decision
- Use API keys formatted as `hub_<token>`.
- Derive hashes using PBKDF2 (SHA-256) with per-key salt and a global pepper.
- Store a short prefix for efficient lookup and verify with constant-time comparison.
- Use `[Flags]` enum scopes (`Read`, `Ingest`, `Admin`) enforced by endpoint filters.

## Alternatives considered
- **Plain SHA hashing**: insufficient resistance to offline attacks.
- **JWT**: introduces signing key management and does not solve one-time key display or simple revocation.
- **OAuth/OIDC**: too heavy for initial ingestion use cases.

## Consequences
- Requires managing a global pepper in configuration.
- API keys are non-recoverable; clients must store the plaintext when created.
- Scope checks are consistent and cheap to evaluate.

## Example
```text
hub_Wx3s2QeN9uR6m7v1KJ0W4Z4e3r2Y8a1p
```
