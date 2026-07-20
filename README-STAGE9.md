# Trading Platform Stage 9

Stage 9 completes the architecture boundary between live trading and backtesting. Business policies are in `TradingSystem.Strategies` and are consumed by both live bot plugins and historical simulators. Bot-specific backtesting is present for BOT8011-BOT8016.

## Shared policies
- `GridSpacingPolicy`: BOT8012, BOT8013, BOT8014 live/backtest.
- `PositionAdmissionPolicy`: reusable side enable, side limit and reverse admission rules.
- `Stop3Policy`: initial STOP3, trailing, breakout and Teeth exit formulas.
- `AlligatorEntryPolicy`: BOT8016 live/backtest entry rule.

## Backtesting coverage
- BOT8011: external signals, reverse, partial TP, initial SL, STOP3 and trailing.
- BOT8012: multi-position TP-only grid.
- BOT8013/BOT8014: shared TP-only grid simulator using the same spacing policy.
- BOT8015: protected partial-TP/STOP3 lifecycle using the common protected simulator behavior.
- BOT8016: Alligator/SMA200 entry, signal-candle protection, breakout STOP3 and Teeth exit.

This package is an architecture-complete baseline, but financial parity must still be verified using recorded production signals/order events and automated golden-master tests before live funds are used.
