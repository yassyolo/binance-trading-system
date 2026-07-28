# Stage 14 migration

Apply migrations in order:

1. `001_trading_history.sql`
2. `002_performance_analytics.sql`
3. `003_risk_and_reconciliation.sql`
4. `004_dashboard_backend.sql`
5. `005_bot_runtime_orchestration.sql`
6. `006_jobs_and_historical_ingestion.sql`

Then start:

```bash
dotnet run --project src/TradingSystem.Jobs.Worker
```

The new worker can run independently from Dashboard API and StrategyService. Configure `ConnectionStrings:TradingHistory`, `JobWorkers`, and `HistoricalDataIngestion` in its appsettings or environment variables.
