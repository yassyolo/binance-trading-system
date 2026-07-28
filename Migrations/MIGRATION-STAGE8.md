# Stage 8 migration

Use Stage 8 as the new complete baseline.

Remove any separate projects or files named:

- `TradingSystem.Backtesting.Bot8011` from the uploaded prototype;
- `TradingSystem.Backtesting.Bot8012` from the uploaded prototype;
- duplicate `HistoricalCandle`, `Candle`, or `BacktestSide` types;
- bot-specific copies of CSV candle loaders and HTML report writers;
- `ITradingHistoryStore` / JSONL history added only for backtesting.

Use:

```powershell
dotnet restore TradingPlatform.Stage8.slnx
dotnet build TradingPlatform.Stage8.slnx
dotnet test tests/TradingSystem.Backtesting.Bots.Tests
```

Run BOT8011:

```powershell
dotnet run --project src/TradingSystem.BotBacktesting.Cli -- --bot BOT8011 --candles data/BTCUSDC_1m.csv --signals data/BOT8011_signals.csv
```

Run BOT8012 optimization:

```powershell
dotnet run --project src/TradingSystem.BotBacktesting.Cli -- --bot BOT8012 --candles data/BTCUSDC_1m.csv --signals data/BOT8012_signals.csv --optimize --top 50
```
