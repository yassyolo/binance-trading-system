# Stage 6 migration

1. Use this folder as the new baseline; it already includes Stages 1–5.
2. Remove the old `StrategyService.Configuration/Execution/Positions/Orders/Services/Strategies` files for BOT8013–BOT8016.
3. Do not keep the old `OrderExecutionService`, `PositionLockService`, duplicated client-id helpers or bot-specific Binance historical/order clients.
4. Copy the four `Bots/Bot801x` folders and `Bots/Common/TpOnlyGrid` only when merging manually.
5. Add the four `AddBot801x` calls shown in `Program.cs`.
6. Copy the `Bots` configuration sections from `StrategyService/appsettings.json`.
7. Run restore/build and resolve any exchange DTO differences against the exact Binance client version in your repository.
