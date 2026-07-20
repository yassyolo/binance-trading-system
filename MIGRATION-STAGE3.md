# Migration Stage 3

Delete the old `AlligatorIndicatorService` and `BollingerIndicatorService` hosts after copying configuration into `IndicatorServices`.

Move/replace:
- both `BinanceHistoricalKlineClient` classes -> `TradingSystem.Binance.Market.BinanceHistoricalCandleSource`.
- `Candle` and `BollingerCandle` -> `TradingSystem.Domain.MarketData.MarketCandle`.
- indicator payload classes -> common `TradingSystem.Contracts.Indicators.IndicatorSnapshotMessage`.
- both indicator Workers/subscribers/history initializers -> `IndicatorServices.Worker`.
- direct Redis state/publish code -> `IRedisStatePublisher`.

Keep `MarketDataService`, but replace its old Program/Worker/Publisher with the Stage 3 versions.

Configuration paths change from `Alligator` and `Bollinger` to `Indicators:Alligator` and `Indicators:Bollinger`.
