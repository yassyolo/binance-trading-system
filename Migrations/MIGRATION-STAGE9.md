# Stage 9 migration

1. Replace Stage 8 with this complete package.
2. Restore and build `TradingPlatform.Stage9.slnx`.
3. Run all tests.
4. Export recorded signals from PostgreSQL and run BOT8011-BOT8016 backtests.
5. Compare live decisions with backtest decisions by signal id. Differences must be resolved before optimization.
6. Keep strategy formulas in `TradingSystem.Strategies`; do not add copies inside CLI or report projects.

Commands:
```powershell
dotnet restore TradingPlatform.Stage9.slnx
dotnet build TradingPlatform.Stage9.slnx
dotnet test
```
