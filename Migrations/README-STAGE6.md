# Trading Platform — Stage 6

Stage 6 extends Stage 5 with BOT8013, BOT8014, BOT8015 and BOT8016.

## Architecture decisions

- BOT8013 and BOT8014 use the shared `Bots/Common/TpOnlyGrid` plugin foundation instead of copied strategy, executor, active-position, order-event and healing implementations.
- BOT8015 uses the shared Binance protected-position execution service and keeps only its STOP3/trailing policy in the bot plugin.
- BOT8016 keeps its candle/Alligator-driven lifecycle isolated as a specialized plugin. Its market subscription is intentionally bot-specific because entry and exit are driven by two different candle timeframes.
- Binance client-order-id parsing, price/quantity rounding and order-side mapping remain in `TradingSystem.Binance`.
- Position locking, healing contracts, Redis state and history recording remain shared infrastructure.

## New registrations

```csharp
services.AddBot8013(configuration);
services.AddBot8014(configuration);
services.AddBot8015(configuration);
services.AddBot8016(configuration);
```
