# Stage 17 — Paper Trading Engine

Stage 17 adds a live-market, zero-exchange-execution mode. Set a bot configuration `Environment` to `Paper`; signals still pass through runtime state, strategy and central risk, but execution is routed to `PaperTradeExecutor` instead of Binance.

## Main flow

`Signal -> TradingEngine -> Strategy -> CentralRiskManager -> EnvironmentAwareTradeExecutor -> Paper/Binance`

Paper positions use the real mark-price provider, configurable slippage and commission, virtual TP/SL, optimistic close concurrency, PostgreSQL persistence and a background fill worker.

## API

- `GET /api/v1/paper/account`
- `GET /api/v1/paper/positions`
- `POST /api/v1/paper/reset` (Administrator)

## Safety

Paper, Demo and Production data are separated. Changing environment remains restart-required in the existing configuration contract; operators should stop the bot and verify no live positions before switching modes.
