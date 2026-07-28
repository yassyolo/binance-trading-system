# Stage 18 - Strategy Plugin System

Stage 18 adds a versioned strategy plugin layer while preserving every existing `ITradingStrategy` implementation and bot registration.

## Runtime flow

`BotRuntimeConfiguration.StrategyType` is now treated as a strategy plugin id. `TradingStrategyResolver` loads the bot configuration and resolves the configured plugin from `TradingStrategyRegistry`. When no configured plugin exists, the resolver safely falls back to the legacy bot-name mapping.

## Components

- `TradingSystem.StrategyPlugins`
- `IStrategyPluginModule`
- `IStrategyPluginCatalog`
- `ITradingStrategyResolver`
- `StrategyPluginLoader`
- plugin metadata and version selection
- external assembly discovery from `plugins/strategies`
- dashboard configuration can change `StrategyType`

## External plugin contract

An external assembly implements `IStrategyPluginModule`, exposes a public parameterless constructor, and registers one or more `ITradingStrategy` implementations in `ConfigureServices`.

The module descriptor must provide a stable `PluginId` and semantic version. Duplicate plugin ids are rejected; when catalog metadata contains several versions, the highest parseable version is selected.

## Safety

- Missing configured plugin causes a controlled failure instead of silently running a different strategy.
- Legacy BOT8011-BOT8016 strategy names remain aliases.
- Symbol compatibility is still validated by `TradingEngine`.
- Risk management and execution routing remain outside plugins.
- Plugins cannot bypass runtime state, cooldown, central risk, paper routing, or audit boundaries.
- External plugin load failures fail startup by default.

## Configuration

```json
"StrategyPlugins": {
  "Enabled": true,
  "PluginDirectory": "plugins/strategies",
  "LoadExternalAssemblies": true,
  "FailOnPluginLoadError": true,
  "DefaultVersion": "1.0.0"
}
```

## Deployment

Copy plugin DLLs and their private dependencies to the configured plugin directory before starting `StrategyService`. Restart is required when adding or replacing an assembly. Runtime switching between already loaded plugins is performed through bot configuration versioning.
