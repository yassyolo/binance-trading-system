# Stage 4 migration

1. Use this ZIP as the new complete baseline; it already contains Stages 1–3.
2. Remove old `StrategyService.Configuration.ServiceCollectionExtensions`, all `Composite*` registrations, `SignalProcessor`, `PositionLockService`, `OrderEventDeduplicationService`, duplicate Binance side/client-id helpers and the old `OrderExecutionService`.
3. Register each future bot through one extension similar to `AddBot8012`.
4. Do not register Redis or Binance clients manually in `StrategyService.Program`; use `AddTradingRedis` and `AddBinanceFutures`.
5. Do not delete positions on terminal events. Mark the domain lifecycle state and persist it; a later retention/archive job may remove closed operational records.
6. The submitted history examples were not retained. Add generic persistence through an engine/event recorder in `TradingSystem.Persistence.PostgreSql`, never through BOT8012-specific decorators.
