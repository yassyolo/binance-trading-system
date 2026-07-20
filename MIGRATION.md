# Migration map

| Old item | New location / action |
|---|---|
| `CompositeTradingStrategy` | Replace with `TradingStrategyRegistry` |
| `CompositeTradeExecutor` | Replace with `TradeExecutorRegistry` |
| `CompositeActivePositionProvider` | Replace with `ActivePositionProviderRegistry` |
| duplicate `ISignalCooldownStore` | Keep one file in `TradingSystem.Application/Engine` |
| `StrategyService.Services.BinanceExchangeInfoService` | Move to `TradingSystem.Binance.Exchange` |
| duplicate `BinanceStartupService` | Keep one file in `TradingSystem.Binance.Startup` |
| hard-coded engine TTL fields | Replace with `TradingEngineOptions` |
| ignored `PositionsToClose` | Engine now closes all requested positions before open |
| generic Binance request exceptions | Replace with `BinanceApiException` |

Do not add BOT8012 code to either project. BOT-specific rules and execution implementations belong under `StrategyService/Strategies/Bot8012` and related plugin folders.
