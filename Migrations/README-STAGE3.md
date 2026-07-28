# Trading Platform Stage 3

Adds a reusable market-data and indicator pipeline on top of Stage 2.

## Projects
- `MarketDataService`: Binance market-stream host; publishes normalized closed-kline contracts.
- `TradingSystem.Indicators`: reusable indicator plugin library.
- `IndicatorServices`: one generic host for all registered indicator processors.
- shared historical candle loading is in `TradingSystem.Binance` behind `IHistoricalCandleSource`.

## Add a new indicator
1. Implement `IIndicatorProcessor` in `TradingSystem.Indicators`.
2. Add options under `Indicators:<Name>`.
3. Register it in `AddTradingIndicators`.
No new Redis worker or Binance client is required.
