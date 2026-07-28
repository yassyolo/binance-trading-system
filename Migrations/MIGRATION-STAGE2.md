# Migration from the pasted files

## Delete/replace duplicates
- Delete the duplicate `ISignalCooldownStore`.
- Delete service-local `HealingSnapshot` models. Use `TradingSystem.Contracts.UserStream.HealingSnapshotMessage`.
- Delete `TradingSystem.Domain.Healing.HealingSnapshot`; healing is a transport snapshot, not a domain entity.
- Delete `UserStreamService.Clients.BinanceOrdersSnapshotClient`; snapshots now reuse `IBinanceFuturesOrderClient` through `BinanceOrdersSnapshotProvider`.
- Move `BinanceListenKeyClient`, `BinanceUserStreamClient` and their options to `TradingSystem.Binance.UserStream`.
- Move `JsonDefaults`, WebSocket helpers and `SystemClock` to `TradingSystem.Infrastructure`.
- Replace service-specific `RedisPublisher` with `IRedisMessagePublisher`.
- Replace `RedisPositionStoreOptions` with common `RedisOptions.KeyPrefix`.

## Important behavioral changes
- `BotPosition` transitions receive an explicit UTC timestamp. This makes live execution and backtesting deterministic.
- Redis signal subscriber consumes `TradingSignalMessage` and maps it to the Domain `TradeSignal`.
- Healing supports multiple configured symbols.
- Redis key scanning checks all connected endpoints and de-duplicates positions.
- UserStreamService does not sign or duplicate order snapshot requests.
