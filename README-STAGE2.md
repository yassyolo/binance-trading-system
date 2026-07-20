# Trading Platform — Stage 2

This archive extends Stage 1 with reusable Contracts, Infrastructure, Redis and UserStreamService projects.

## Ownership
- `TradingSystem.Contracts`: wire messages shared between processes.
- `TradingSystem.Domain`: business entities and enums only.
- `TradingSystem.Application`: use cases and ports.
- `TradingSystem.Infrastructure`: generic technical helpers.
- `TradingSystem.Redis`: Redis adapters, operational state and pub/sub transport.
- `TradingSystem.Binance`: all Binance REST/WebSocket integration.
- `UserStreamService`: host/orchestration only.

## Registration
StrategyService should use:
```csharp
services.AddTradingInfrastructure();
services.AddTradingApplication();
services.AddTradingRedis(configuration, subscribeToSignals: true);
services.AddBinanceFutures(configuration);
```

UserStreamService uses Redis without the strategy-signal subscriber.
