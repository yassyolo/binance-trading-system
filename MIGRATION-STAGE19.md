# Migration Stage 19

Apply after `010_strategy_plugin_system.sql`:

```text
011_event_store.sql
```

The migration creates schema `trading_event_store` and append-only table `events` with:

- global ordering;
- unique event IDs;
- aggregate ordering/versioning;
- correlation and causation IDs;
- indexed bot/signal/position timelines;
- JSONB payload and metadata.

Application database users need `SELECT`, `INSERT` and sequence usage permissions. Do not grant `UPDATE`, `DELETE` or `TRUNCATE` to runtime service accounts.
