# Trading Platform — Stage 20

Latest architecture version: **Stage 20 — Replay Engine**.

See `README-STAGE20.md` and `MIGRATION-STAGE20.md`.

# Trading platform – stage 1

This package rewrites the supplied `TradingSystem.Application` and `TradingSystem.Binance` code into reusable platform projects.

## Included projects

- `TradingSystem.Domain` – only the minimal domain types required by the two projects. Keep this project in the final solution.
- `TradingSystem.Application` – generic trading engine, strategy/executor/provider registries and application contracts.
- `TradingSystem.Binance` – Binance HTTP integration and the adapter implementing `IMarketPriceProvider`.

## Main changes

1. `Composite*` classes are replaced by explicit registries with duplicate registration validation.
2. `TradingEngine` contains no BOT8012 rules.
3. `OpenAfterClosing` is now actually executed before opening the replacement position.
4. Engine TTLs and signal-age limits are configuration options.
5. Duplicate interfaces/classes from the supplied file were removed.
6. `BinanceExchangeInfoService` was moved out of `StrategyService` and now belongs to `TradingSystem.Binance`.
7. Binance configuration, HTTP clients and startup service are registered through one extension method.
8. API failures use `BinanceApiException` instead of generic `InvalidOperationException`.

## Host registration

```csharp
services.Configure<TradingEngineOptions>(
    configuration.GetSection(TradingEngineOptions.SectionName));

services.AddTradingApplication();
services.AddBinanceFutures(configuration);
```

The host still needs infrastructure implementations for:

- `ISignalIdempotencyStore` (later in `TradingSystem.Redis`)
- `ISignalCooldownStore` (later in `TradingSystem.Redis`)
- `ITradingOperationLockProvider` (later in `TradingSystem.Redis`)
- `IPositionStore` (Redis live state, later PostgreSQL history separately)

Each strategy plugin registers:

```csharp
services.AddSingleton<ITradingStrategy, Bot8012Strategy>();
services.AddSingleton<IBotTradeExecutor, Bot8012TradeExecutor>();
services.AddSingleton<IBotActivePositionProvider, Bot8012ActivePositionProvider>();
services.AddSingleton<IBinanceTradingConfiguration>(sp =>
    sp.GetRequiredService<IOptions<Bot8012Options>>().Value);
```

## Files to replace from the old code

Replace the old `CompositeTradingStrategy`, `CompositeTradeExecutor`, and `CompositeActivePositionProvider` with the registry classes in this package. Remove duplicate copies of `ISignalCooldownStore` and `BinanceStartupService`.

`TradingSystem.Binance` should never contain a namespace beginning with `StrategyService`.

## Validation

The archive was structurally checked, but `dotnet build` could not be run in the generation environment because the .NET SDK was unavailable.


## Latest stage
See `README-STAGE5.md`.


## Stage 6

See `README-STAGE6.md` and `MIGRATION-STAGE6.md`.

## Stage 7

Stage 7 adds historical range loading, a backtesting engine, reports, CLI tools and tests. See `README-STAGE7.md` and `MIGRATION-STAGE7.md`.


## Stage 8

See `README-STAGE8.md` for BOT8011/BOT8012 backtesting and optimization.


## Stage 9
See `README-STAGE9.md` for shared live/backtest policies and BOT8011-BOT8016 parity architecture.


Stage 10: see `README-STAGE10.md`.


## Stage 11
See `README-STAGE11.md` for central risk management and reconciliation/healing.


## Stage 16

Dashboard API hardening is documented in `README-STAGE16.md`.


## Stage 17

See `README-STAGE17.md` for the Paper Trading Engine.


## Stage 18
See `README-STAGE18.md` for the Strategy Plugin System.


## Stage 19

Append-only Event Store and Trading Timeline are documented in `README-STAGE19.md`.
