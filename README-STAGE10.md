# Trading Platform Stage 10

Stage 10 adds production-oriented optimization and PostgreSQL performance analytics on top of Stage 9.

## New projects

- `TradingSystem.Optimization` — parameter tuning, scoring and walk-forward validation.
- `TradingSystem.Analytics` — performance run, snapshot, trial and walk-forward contracts.
- `TradingSystem.Optimization.Cli` — executable examples for grid search and walk-forward.
- `TradingSystem.Optimization.Tests` — optimization scoring tests.

## Optimization flow

1. Build a parameter space for BOT8011–BOT8016.
2. Run every candidate over an in-sample period.
3. Calculate a risk-adjusted score.
4. Select the best candidate.
5. Run it over the next unseen out-of-sample window.
6. Slide the window and repeat.
7. Persist trials, selected parameters and out-of-sample results to PostgreSQL.

## PostgreSQL analytics

Apply `Migrations/002_performance_analytics.sql` after the Stage 5 history migration. It creates:

- `performance_runs`
- `performance_snapshots`
- `performance_trades`
- `optimization_trials`
- `walk_forward_windows`
- `v_bot_performance_summary`

Operational event history remains in the Stage 5 tables. Aggregated performance and research results are stored separately.

## Build

```powershell
dotnet restore TradingPlatform.Stage10.slnx
dotnet build TradingPlatform.Stage10.slnx
dotnet test
```

## Grid search example

```powershell
dotnet run --project src/TradingSystem.Optimization.Cli -- `
  --candles data/BTCUSDC_1m.csv `
  --signals data/BOT8012_signals.csv `
  --top 20
```

## Walk-forward example

```powershell
dotnet run --project src/TradingSystem.Optimization.Cli -- `
  --candles data/BTCUSDC_1m.csv `
  --signals data/BOT8012_signals.csv `
  --walk-forward `
  --train-bars 10000 `
  --test-bars 2000 `
  --step-bars 2000
```
