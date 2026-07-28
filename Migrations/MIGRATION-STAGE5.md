# Stage 5 migration
1. Use this directory as the new baseline; it already includes Stages 1-4.
2. Remove old history types from `TradingSystem.Signals.History`; use `TradingSystem.Observability.History`.
3. Remove bot-specific history decorators and `dynamic` helpers.
4. Remove the old `OrderExecutionService` BOT8011 methods; protected entry is now `BinanceProtectedPositionService`.
5. Remove `PositionLockService`; use `IPositionLockProvider` / `RedisPositionLockProvider`.
6. Remove in-memory signal-generation timestamps; use `IDistributedSignalThrottleStore`.
7. Put BOT8011 files under `StrategyService/Bots/Bot8011` and register only `services.AddBot8011(configuration)`.
8. Apply `001_trading_history.sql` before enabling PostgreSQL recording.
9. Do not delete closed Redis positions during event handling. Keep terminal state for reconciliation; archive/cleanup separately.

Not included yet: PnL/fee calculation, DashboardApi endpoints, durable event bus, and full BOT8011 manual-position recovery. Those should be separate platform features rather than hidden startup side effects.
