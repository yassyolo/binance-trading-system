# Migration to Stage 12
1. Build `TradingPlatform.Stage12.slnx`.
2. Apply `004_dashboard_backend.sql` after the previous migrations.
3. Replace the development JWT signing key and database password.
4. Register heartbeats from every service in `trading_dashboard.service_heartbeats`.
5. Add a StrategyService worker to process `bot_commands` and an optimization worker to process `jobs` before enabling write controls in Production.
6. Keep React read-only until role-based authentication and command processing are integration-tested on Binance Demo/Testnet.
