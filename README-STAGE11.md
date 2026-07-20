# Stage 11 — Central Risk, Reconciliation and Healing

Stage 11 inserts a central risk gate between strategy decisions and execution, and adds a recurring reconciliation pipeline comparing Redis/local positions with Binance positions and orders.

## Safety defaults
- Protective orders are never recreated automatically by default.
- Quantity mismatches and orphan exchange exposure require manual review.
- Only stale local state with no exchange exposure/orders is auto-healed by default.
- Critical unresolved reconciliation findings may block all new entries.

## Build
```powershell
dotnet restore TradingPlatform.Stage11.slnx
dotnet build TradingPlatform.Stage11.slnx
dotnet test
```

Apply migrations in order: `001`, `002`, `003_risk_and_reconciliation.sql`.
