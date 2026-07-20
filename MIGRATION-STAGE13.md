# Stage 12 → Stage 13 migration

## Database

Apply after migrations 001–004:

```text
src/TradingSystem.Persistence.PostgreSql/Migrations/005_bot_runtime_orchestration.sql
```

The migration adds:

- independent runtime-state versioning;
- runtime actor, reason and timestamp;
- execution enable flag;
- command worker ownership;
- retry counters and retry scheduling;
- indexes for concurrent command claiming;
- configuration refresh audit table.

Existing bots remain `Stopped` with execution disabled after migration. Start them explicitly from the Dashboard API after verifying Demo/Testnet configuration.

## Configuration

`StrategyService/appsettings.json` contains:

```json
"BotRuntime": {
  "Enabled": true,
  "CommandPollSeconds": 2,
  "CommandBatchSize": 20,
  "CommandProcessingTimeoutSeconds": 60,
  "ConfigurationRefreshSeconds": 5,
  "MaximumCommandAttempts": 5
}
```

## Safety

Test `Start`, `Pause`, `Resume`, `Stop`, and `EmergencyStop` in Demo/Testnet. Position-level commands are not executed by the runtime worker and return `Rejected` until a dedicated protected command handler is introduced.
