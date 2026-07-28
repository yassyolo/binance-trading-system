# Stage 16 — Dashboard API Hardening

Stage 16 hardens the existing Dashboard backend without changing its `/api/v1` contracts.

## Included

- RFC 7807 Problem Details and a global exception handler.
- Correlation IDs through `X-Correlation-ID`.
- Structured request logging without request-body logging.
- Explicit request validation and bounded pagination.
- JWT validation with short clock skew and deny-by-default authorization.
- Viewer, Operator and Administrator policies.
- Explicit CORS origins, methods and headers.
- Fixed-window rate limits for read, write and dangerous operations.
- `X-Idempotency-Key` protection for bot commands, backtests and optimizations.
- PostgreSQL-backed idempotent response replay for 24 hours.
- HTTPS redirection, HSTS and security response headers outside Development.
- Swagger Bearer authentication and production opt-in.
- One-megabyte request-body limit.
- Audit payload redaction and size limiting.

## Idempotency

The following POST endpoints require a unique `X-Idempotency-Key` between 16 and 128 characters:

- `/api/v1/bots/{botName}/commands`
- `/api/v1/backtests`
- `/api/v1/optimizations`

Retrying the same request with the same user, key and payload replays the stored response and returns `X-Idempotent-Replay: true`. Reusing the key with a different payload returns HTTP 409.

## Production requirements

- Replace the development JWT signing key with a secret of at least 32 characters.
- Configure explicit HTTPS React origins under `Cors:Origins`.
- Keep Swagger disabled, or explicitly set `Swagger:Enabled=true` for a protected internal environment.
- Apply migration `008_api_hardening.sql`.
- Configure trusted proxy/network settings when deploying behind Synology reverse proxy.
