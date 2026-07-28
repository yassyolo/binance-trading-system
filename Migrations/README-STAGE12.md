# Stage 12 — Dashboard Backend

Stage 12 adds the .NET backend that a future React dashboard will consume.

## Projects
- `TradingSystem.Dashboard.Contracts` — stable API DTOs.
- `TradingSystem.Dashboard.Application` — ports and validation.
- `TradingSystem.Dashboard.Api` — ASP.NET Core REST API, JWT roles, Swagger and CORS.
- PostgreSQL dashboard store and migration `004_dashboard_backend.sql`.

## Main API groups
- `/api/v1/overview`, `/bots`, `/signals`, `/positions`, `/trades`
- `/analytics`, `/analytics/equity`, `/charts`
- `/backtests`, `/optimizations`, `/runs`, `/comparisons`
- `/health/components`, `/alerts`
- Bot commands and configuration use optimistic concurrency and audit fields.

## Safety model
HTTP never sends Binance orders directly. Trading actions are inserted into `trading_dashboard.bot_commands`. A StrategyService command worker must claim and execute them through the existing risk/execution/reconciliation pipeline. Emergency and position actions require explicit confirmation.

## Database migrations
Run in order: `001`, `002`, `003`, `004_dashboard_backend.sql`.

## Run
`dotnet run --project src/TradingSystem.Dashboard.Api`
Swagger: `/swagger`
