# Trading Platform — Stage 7

Stage 7 adds a reusable backtesting and historical-data toolchain to the Stage 6 trading platform.

## Added projects

- `TradingSystem.Backtesting` — deterministic simulation engine, portfolio, fills, costs, risk sizing, metrics and strategy registry.
- `TradingSystem.Backtesting.Reporting` — JSON, CSV, HTML and Excel reports.
- `TradingSystem.HistoricalData` — CSV historical candle source.
- `TradingSystem.Backtesting.Cli` — command-line backtest runner using CSV or Binance data.
- `TradingSystem.HistoricalData.Cli` — Binance historical candle downloader.
- `TradingSystem.Backtesting.Tests` — initial execution-policy tests.

## Important architecture decisions

- Backtests reuse `TradingSystem.Domain.MarketData.MarketCandle`; there is no duplicate `HistoricalCandle` model.
- Range loading is defined by `IHistoricalCandleRangeSource` in Application.
- Binance range loading is implemented in `TradingSystem.Binance`.
- CSV parsing is implemented in `TradingSystem.HistoricalData`.
- Existing PostgreSQL pipeline history remains the production history implementation. The older JSON-lines history implementation from the supplied fragment was intentionally not added.
- Backtest strategies are plugins registered through `IBacktestStrategyFactory`.

## Run a sample backtest

```powershell
dotnet run --project src/TradingSystem.Backtesting.Cli -- \
  --strategy EMA_CROSS \
  --source csv \
  --file samples/BTCUSDC_1h_sample.csv \
  --symbol BTCUSDC \
  --interval 1h \
  --balance 10000 \
  --risk 1 \
  --param.fast 20 \
  --param.slow 50 \
  --param.slPercent 1 \
  --param.tpPercent 2
```

## Download historical candles

```powershell
dotnet run --project src/TradingSystem.HistoricalData.Cli -- \
  --symbol BTCUSDC \
  --interval 5m \
  --from 2025-01-01 \
  --to 2026-01-01 \
  --output data/BTCUSDC_5m_2025.csv
```
