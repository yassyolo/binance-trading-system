# Migrating from Stage 10
1. Use Stage 11 as the new baseline.
2. Apply `003_risk_and_reconciliation.sql`.
3. Configure `CentralRisk` and `Reconciliation` in StrategyService appsettings.
4. Keep `AutoHealProtectiveOrders=false` until exchange testnet integration tests pass.
5. Run reconciliation against Binance testnet before production.
