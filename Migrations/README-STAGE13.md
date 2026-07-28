# Trading Platform Stage 13 — Bot Runtime Orchestration

Stage 13 extends Stage 12 with the runtime integration required for dashboard bot controls.

## Added

- `TradingSystem.BotRuntime` shared project.
- PostgreSQL-backed bot runtime state with optimistic concurrency.
- `BotCommandWorker` in `StrategyService`.
- Safe `Start`, `Stop`, `Pause`, `Resume`, and `EmergencyStop` transitions.
- Idempotent PostgreSQL command claiming with `FOR UPDATE SKIP LOCKED`.
- Processing timeout recovery, bounded retry, attempt count, and terminal command states.
- `BotConfigurationRefreshWorker` with version-aware in-memory snapshots.
- Runtime gating in `TradingEngine` before strategy and execution.
- Dynamic configuration use for enable flags, symbol, cooldown, and TP-only grid parameters.
- Stage 13 migration and runtime state tests.

## Runtime semantics

- `Running`: accepts new signals and execution is enabled.
- `Paused`: blocks new entries; user-stream, protection, reconciliation, and healing continue.
- `Stopped`: blocks new entries; existing position protection remains active.
- `EmergencyStopped`: blocks new entries and disables execution until an explicit resume/start command after operator review.
- `Faulted`: blocks new entries and requires operational investigation.

Stopping or pausing a bot does not silently close existing positions.

## Command processing

Dashboard API inserts a command into `trading_dashboard.bot_commands`. `BotCommandWorker` claims rows transactionally and updates runtime state. Multiple StrategyService instances can run because claims use PostgreSQL row locking with `SKIP LOCKED`.

Position commands (`ClosePosition`, `CancelTakeProfit`, `RecreateTakeProfit`) are deliberately rejected by this worker until the protected position-command handler is implemented and validated on Binance Demo/Testnet.

## Dynamic configuration

The refresh worker checks changed configuration versions and updates a thread-safe process snapshot. The trading engine uses dynamic symbol, direction flags and cooldown. TP-only grid bots also use dynamic quantity, price/profit distance and order-side limit for new decisions/orders.

`Environment`, credentials and connection-level settings remain restart-required. Existing positions retain the parameters with which they were created.

## Run

```powershell
dotnet restore TradingPlatform.Stage13.slnx
dotnet build TradingPlatform.Stage13.slnx
dotnet test
```

Apply migrations in order through `005_bot_runtime_orchestration.sql` before starting StrategyService.
