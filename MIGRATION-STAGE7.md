# Stage 7 migration

Use Stage 7 as the new complete baseline; it already contains Stages 1–6.

## Remove or do not copy

- duplicate `HistoricalCandle` records;
- a second Binance historical HTTP client outside `TradingSystem.Binance`;
- `IHistoricalCandleSource` range variants inside backtesting projects;
- `JsonLinesTradingHistoryStore`, `TradingHistoryRecorder` and the older dashboard-history records from the supplied fragment.

The platform already has structured `ITradingPipelineRecorder` contracts and PostgreSQL persistence from Stage 5. Keeping both history systems would create two competing sources of truth.

## Project placement

- simulation logic → `TradingSystem.Backtesting`;
- report rendering → `TradingSystem.Backtesting.Reporting`;
- Binance historical range access → `TradingSystem.Binance`;
- CSV access → `TradingSystem.HistoricalData`;
- executable workflows → the two CLI projects.

## Build

```powershell
dotnet restore TradingPlatform.Stage7.slnx
dotnet build TradingPlatform.Stage7.slnx
dotnet test tests/TradingSystem.Backtesting.Tests
```
