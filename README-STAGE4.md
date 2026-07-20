# Stage 4 — Signals and StrategyService

This stage extends Stage 3 with a reusable `TradingSystem.Signals` library and a clean `StrategyService` host. BOT8012 is the first plugin implementation and no common engine code contains BOT8012 branches.

## Runtime flow
MarketDataService -> closed kline -> IndicatorServices -> indicator snapshots -> signal coordinator/generator -> `trading:signals` -> StrategyService -> TradingEngine -> BOT executor -> Binance -> UserStreamService -> order/healing events -> StrategyService.

## Important placement decisions
- Binance client-order-id parsing, side mapping, fill waiting and TP-only execution are in `TradingSystem.Binance`.
- Signal abstractions and models are in `TradingSystem.Signals`.
- Order-event deduplication is a Redis-backed application port, not an in-memory StrategyService helper.
- Healing uses the shared `HealingSnapshotMessage` contract.
- BOT8012 code is grouped as one feature plugin under `StrategyService/Bots/Bot8012`.
- History decorators and `dynamic` examples from the input were intentionally not copied. Generic PostgreSQL/observability recording will be added as infrastructure decorators around the engine, not per bot.
