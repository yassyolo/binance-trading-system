# Trading Platform — Stage 8

Stage 8 adds bot-specific backtesting for BOT8011 and BOT8012 without duplicating the common candle, side, metrics, reporting, historical-data, or optimization infrastructure.

## Added projects

- `TradingSystem.Backtesting.Bots`
- `TradingSystem.BotBacktesting.Cli`
- `TradingSystem.Backtesting.Bots.Tests`

## BOT8011 simulator

Simulates external historical signals, one active position, reverse-on-opposite-signal, fixed initial stop-loss distance, partial take profit, STOP3 creation, trailing STOP3, fees, slippage, cooldown, margin checks, and intrabar TP/SL conflict policy.

## BOT8012 simulator

Simulates multiple same-side TP-only positions, the live grid-gap formula, side limits, cooldown, fees, slippage, mark-to-market equity, take-profit fills, and forced close at the backtest end.

## Shared infrastructure

Both bots use:

- `MarketCandle` from Domain;
- `TradeSide` and common backtesting types;
- `HistoricalBotSignal`;
- common executions, positions, decisions, metrics, and equity records;
- `GridSearchOptimizer`;
- `BotBacktestReportWriter`;
- `CsvHistoricalCandleSource`.

No second `HistoricalCandle`, `Candle`, `BacktestSide`, JSONL history store, or duplicate report writer was introduced.
