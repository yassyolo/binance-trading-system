# Stage 19 — Event Store and Trading Timeline

Stage 19 adds an append-only event log without replacing the existing operational state tables.

## Design

- PostgreSQL `trading_event_store.events` is an immutable chronological log.
- Existing position, trade, runtime, job and alert tables remain the read-model/source of truth for current state.
- Events have a global position and a per-aggregate version.
- Correlation and causation IDs reconstruct one complete trading flow.
- Payload and metadata are versioned JSON documents.

## Captured trading pipeline events

- `trading.signal.received`
- `trading.strategy.decision-taken`
- `trading.risk.decision-taken`
- `trading.execution.requested`
- `trading.execution.completed`
- `trading.execution.failed`

The model also defines event names for positions, runtime configuration, alerts and jobs so those publishers can be connected incrementally without changing the storage contract.

## Dashboard API

- `GET /api/v1/timeline`
- `GET /api/v1/event-streams/{aggregateType}/{aggregateId}`

Timeline filters include bot, symbol, position, signal, correlation, event type and time range.

## Important boundary

This stage is not full Event Sourcing. Services do not rebuild operational state from events during startup. The event store is used for audit-grade traceability, timeline views, diagnostics, replay input and later projections.
