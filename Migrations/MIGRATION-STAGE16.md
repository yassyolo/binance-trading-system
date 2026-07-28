# Migration to Stage 16

Apply migrations in order through:

```text
008_api_hardening.sql
```

The migration creates `trading_dashboard.api_idempotency_keys`, which stores request hashes and bounded HTTP responses for protected POST requests. Entries expire after 24 hours. Schedule periodic deletion of expired rows in the operational cleanup job.

Before Production startup, configure a non-development JWT signing key and explicit CORS origins. The API now fails fast when these security requirements are missing.
