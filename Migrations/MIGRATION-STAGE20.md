# Stage 20 migration

Apply after Stage 19:

```text
src/TradingSystem.Persistence.PostgreSql/Migrations/012_replay_engine.sql
```

The migration creates:

- `trading_replay.jobs` — replay lifecycle, filters, progress and cancellation;
- `trading_replay.checkpoints` — resumable cursor and serialized state;
- `trading_replay.steps` — immutable per-source-event replay result;
- `trading_replay.results` — immutable summary and deterministic hash.

Recommended runtime grants:

```sql
GRANT USAGE ON SCHEMA trading_replay TO trading_runtime;
GRANT SELECT, INSERT, UPDATE ON trading_replay.jobs TO trading_runtime;
GRANT SELECT, INSERT, UPDATE ON trading_replay.checkpoints TO trading_runtime;
GRANT SELECT, INSERT ON trading_replay.steps TO trading_runtime;
GRANT SELECT, INSERT ON trading_replay.results TO trading_runtime;
```

Do not grant UPDATE/DELETE/TRUNCATE on `steps` or `results` to the normal runtime role.

Migration order:

```text
001_trading_history.sql
002_performance_analytics.sql
003_risk_and_reconciliation.sql
004_dashboard_backend.sql
005_bot_runtime_orchestration.sql
006_jobs_and_historical_ingestion.sql
007_service_health_alerts_audit.sql
008_api_hardening.sql
009_paper_trading.sql
010_strategy_plugin_system.sql
011_event_store.sql
012_replay_engine.sql
```
