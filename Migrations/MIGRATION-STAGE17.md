# Stage 17 migration

Apply `009_paper_trading.sql` after migrations 001-008.

The migration creates `trading_paper.positions`, `trading_paper.reset_events`, indexes for open/history queries, and permits `Paper`, `Demo` and `Production` in bot configuration environments.

Paper data is never submitted to Binance and is never mixed with exchange reconciliation state.
