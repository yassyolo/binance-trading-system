# Stage 14 — Backtesting, Optimization and Historical Data Workers

Stage 14 closes the Dashboard job loop introduced in Stage 12.

## Added

- `TradingSystem.JobOrchestration` neutral job and historical-data contracts.
- `TradingSystem.Jobs.Worker` hosted service.
- `BacktestingJobWorker` claims `Backtest` jobs with PostgreSQL `FOR UPDATE SKIP LOCKED`, runs BOT8011–BOT8016 engines and persists runs, snapshots and trades.
- `OptimizationJobWorker` executes dashboard-defined BOT8012 parameter ranges, including optional walk-forward validation, and persists trials/windows.
- `HistoricalDataIngestionWorker` downloads Binance Futures klines, performs idempotent upserts and detects missing candle ranges.
- Retry, stale-processing recovery, progress fields and result run IDs for dashboard jobs.

## Runtime flow

Dashboard API -> `trading_dashboard.jobs` -> worker claim -> historical candles/signals -> engine -> analytics tables -> completed job.

Historical ingestion uses PostgreSQL as the shared source for charting, backtesting and optimization. The worker intentionally refuses to run a simulation when the requested candles or signals are missing.

## Safety and scope

The worker does not execute live orders. It may run beside StrategyService. Demo/Production in `HistoricalDataIngestion` selects the Binance market-data endpoint only; no API credentials are stored in dashboard tables.

Dashboard range optimization currently binds BOT8012 parameters (`ProfitDistance`, `PriceDistance`, `CooldownSeconds`, `OrderSideLimit`). BOT8011 and BOT8013–BOT8016 retain their existing CLI optimization spaces until explicit dashboard range binders are added.
