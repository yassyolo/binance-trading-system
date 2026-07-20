# Stage 9 to Stage 10 migration

1. Use Stage 10 as the new baseline.
2. Apply `001_trading_history.sql` if it has not been applied.
3. Apply `002_performance_analytics.sql`.
4. Register `AddTradingAnalytics()` and `AddTradingOptimization()` in research/CLI hosts.
5. Keep live trading services independent from the optimization CLI.
6. Store only completed and reproducible runs in PostgreSQL; keep raw generated report files as immutable artifacts.
7. For production tuning, use chronological train/test splits and never select parameters using the out-of-sample period.

The PostgreSQL analytics store is registered by `AddPostgresTradingHistory()` through `IPerformanceAnalyticsStore`.
