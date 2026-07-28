# Stage 18 migration

1. Apply `010_strategy_plugin_system.sql` after migrations 001-009.
2. Deploy the new `TradingSystem.StrategyPlugins` assembly with `StrategyService`.
3. Keep existing `strategy_type` values for a no-behavior-change deployment.
4. Place optional external strategy assemblies in `plugins/strategies`.
5. Restart StrategyService to discover new assemblies.
6. Change a bot's `StrategyType` only while the bot is stopped and after confirming the plugin supports the configured symbol.

The migration is additive and does not rewrite existing bot configurations.
