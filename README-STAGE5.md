# Trading Platform Stage 5
Stage 5 includes every Stage 4 project and adds:
- `TradingSystem.Observability`: reusable trading-pipeline history contracts and environment abstraction.
- `TradingSystem.Persistence.PostgreSql`: Dapper/Npgsql recorder, query service, and initial schema migration.
- distributed internal-signal throttling in Redis.
- BOT8011 as a StrategyService plugin.
- reusable Binance protected-position execution for entry + partial TP + initial SL.
- ownership-safe Redis position locks used by STOP3 lifecycle and trailing.

## Apply database migration
Run `src/TradingSystem.Persistence.PostgreSql/Migrations/001_trading_history.sql` against the database configured as `ConnectionStrings:TradingDatabase`.

## Startup registration
`StrategyService/Program.cs` shows the complete order. `AddTradingObservability()` is called before `AddPostgresTradingHistory()`, allowing PostgreSQL to replace the null recorder.

## Build
`dotnet restore TradingPlatform.Stage5.slnx`
`dotnet build TradingPlatform.Stage5.slnx`
