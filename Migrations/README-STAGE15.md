# Stage 15 — Operational visibility and audit

Adds shared service heartbeats, a deduplicating/resolving alerts engine, and append-only dashboard audit events.

## Runtime flow
- Every executable publishes a heartbeat to PostgreSQL.
- TradingSystem.Jobs.Worker hosts AlertEngineWorker and evaluates stale services, critical reconciliation findings and failed jobs.
- Dashboard write requests are recorded by AuditMiddleware after completion with actor, correlation ID, status and request payload. Secrets must never be accepted by dashboard contracts.

## Safety
Alerts are upserted by deduplication key and resolved when their source condition disappears. Acknowledgement and resolution are separate concepts.
