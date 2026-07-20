using System.Data;
using System.Text.Json;
using Dapper;
using TradingSystem.BotRuntime.Commands;
using TradingSystem.BotRuntime.Configuration;
using TradingSystem.BotRuntime.Runtime;
using TradingSystem.Persistence.PostgreSql.Connections;

namespace TradingSystem.Persistence.PostgreSql.BotRuntime;

public sealed class PostgresBotRuntimeStateStore(ITradingDbConnectionFactory factory) : IBotRuntimeStateStore
{
    public async Task<BotRuntimeState?> GetAsync(string botName,  CancellationToken cancellationToken)
    {
        await using var connection  =  await factory.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<BotRuntimeState>(new CommandDefinition(
            """
            select bot_name BotName,  runtime_status Status,  runtime_version Version, 
                   runtime_updated_at_utc UpdatedAtUtc,  runtime_updated_by UpdatedBy, 
                   runtime_reason Reason,  execution_enabled ExecutionEnabled
            from trading_dashboard.bot_configurations
            where bot_name  =  @botName
            """,  new { botName },  cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyCollection<BotRuntimeState>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var connection  =  await factory.OpenAsync(cancellationToken);
        return (await connection.QueryAsync<BotRuntimeState>(new CommandDefinition(
            """
            select bot_name BotName,  runtime_status Status,  runtime_version Version, 
                   runtime_updated_at_utc UpdatedAtUtc,  runtime_updated_by UpdatedBy, 
                   runtime_reason Reason,  execution_enabled ExecutionEnabled
            from trading_dashboard.bot_configurations
            order by bot_name
            """,  cancellationToken: cancellationToken))).AsList();
    }

    public async Task<BotRuntimeState> TransitionAsync(string botName,  BotRuntimeStatus status,  long expectedVersion,  string user,  string reason,  bool executionEnabled,  CancellationToken cancellationToken)
    {
        await using var connection  =  await factory.OpenAsync(cancellationToken);
        var updated  =  await connection.QuerySingleOrDefaultAsync<BotRuntimeState>(new CommandDefinition(
            """
            update trading_dashboard.bot_configurations
            set runtime_status  =  @status, 
                execution_enabled  =  @executionEnabled, 
                runtime_version  =  runtime_version + 1, 
                runtime_updated_at_utc  =  now(), 
                runtime_updated_by  =  @user, 
                runtime_reason  =  @reason
            where bot_name  =  @botName and runtime_version  =  @expectedVersion
            returning bot_name BotName,  runtime_status Status,  runtime_version Version, 
                      runtime_updated_at_utc UpdatedAtUtc,  runtime_updated_by UpdatedBy, 
                      runtime_reason Reason,  execution_enabled ExecutionEnabled
            """,  new { botName,  status  =  status.ToString(),  expectedVersion,  user,  reason,  executionEnabled },  cancellationToken: cancellationToken));
        return updated ?? throw new DBConcurrencyException($"Runtime state for '{botName}' was changed concurrently.");
    }
}

public sealed class PostgresBotRuntimeConfigurationStore(ITradingDbConnectionFactory factory) : IBotRuntimeConfigurationStore
{
    private const string Projection  =  """
        select bot_name BotName,  strategy_type StrategyType,  symbol Symbol,  environment Environment, 
               signal_source SignalSource,  enable_long EnableLong,  enable_short EnableShort, 
               quantity Quantity,  leverage Leverage,  price_distance PriceDistance, 
               profit_distance ProfitDistance,  order_side_limit OrderSideLimit, 
               cooldown_seconds CooldownSeconds,  version Version,  updated_at_utc UpdatedAtUtc, 
               restart_required RestartRequired
        from trading_dashboard.bot_configurations
        """;

    public async Task<BotRuntimeConfiguration?> GetAsync(string botName,  CancellationToken cancellationToken)
    {
        await using var connection  =  await factory.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<BotRuntimeConfiguration>(new CommandDefinition(
            Projection + " where bot_name  =  @botName",  new { botName },  cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyCollection<BotRuntimeConfiguration>> GetChangedSinceAsync(DateTime changedSinceUtc,  CancellationToken cancellationToken)
    {
        await using var connection  =  await factory.OpenAsync(cancellationToken);
        return (await connection.QueryAsync<BotRuntimeConfiguration>(new CommandDefinition(
            Projection + " where updated_at_utc > @changedSinceUtc order by updated_at_utc",  new { changedSinceUtc },  cancellationToken: cancellationToken))).AsList();
    }
}

public sealed class PostgresBotCommandQueue(ITradingDbConnectionFactory factory) : IBotCommandQueue
{
    public async Task<IReadOnlyCollection<BotCommand>> ClaimPendingAsync(string workerId,  int batchSize,  TimeSpan processingTimeout,  CancellationToken cancellationToken)
    {
        await using var connection  =  await factory.OpenAsync(cancellationToken);
        await using var transaction  =  await connection.BeginTransactionAsync(cancellationToken);
        var rows  =  (await connection.QueryAsync<CommandRow>(new CommandDefinition(
            """
            with candidates as (
                select command_id
                from trading_dashboard.bot_commands
                where (status  =  'Pending' and next_attempt_at_utc <= now())
                   or (status  =  'Processing' and processing_started_at_utc < now() - @processingTimeout)
                order by requested_at_utc
                for update skip locked
                limit @batchSize
            )
            update trading_dashboard.bot_commands c
            set status  =  'Processing',  processing_started_at_utc  =  now(),  processing_worker_id  =  @workerId, 
                attempt_count  =  attempt_count + 1
            from candidates
            where c.command_id  =  candidates.command_id
            returning c.command_id CommandId,  c.bot_name BotName,  c.command Command, 
                      c.status Status,  c.requested_by RequestedBy,  c.reason Reason, 
                      c.payload::text Payload,  c.requested_at_utc RequestedAtUtc, 
                      c.attempt_count AttemptCount
            """,  new { workerId,  batchSize  =  Math.Clamp(batchSize,  1,  100),  processingTimeout },  transaction,  cancellationToken: cancellationToken))).AsList();
        await transaction.CommitAsync(cancellationToken);
        return rows.Select(x  =>  new BotCommand(x.CommandId,  x.BotName,  Enum.Parse<BotCommandType>(x.Command,  true),  Enum.Parse<BotCommandStatus>(x.Status,  true),  x.RequestedBy,  x.Reason,  JsonDocument.Parse(x.Payload),  x.RequestedAtUtc,  x.AttemptCount)).ToArray();
    }

    public Task CompleteAsync(Guid commandId,  string workerId,  CancellationToken cancellationToken)
         =>  UpdateTerminalAsync(commandId,  workerId,  "Completed",  null,  cancellationToken);

    public async Task FailAsync(Guid commandId,  string workerId,  string error,  bool retryable,  CancellationToken cancellationToken)
    {
        await using var connection  =  await factory.OpenAsync(cancellationToken);
        var status  =  retryable ? "Pending" : "Failed";
        await connection.ExecuteAsync(new CommandDefinition(
            """
            update trading_dashboard.bot_commands
            set status  =  @status,  error  =  @error, 
                next_attempt_at_utc  =  case when @retryable then now() + make_interval(secs  =>  least(300,  power(2,  greatest(attempt_count, 1))::int)) else next_attempt_at_utc end, 
                completed_at_utc  =  case when @retryable then null else now() end, 
                processing_worker_id  =  null
            where command_id  =  @commandId and processing_worker_id  =  @workerId and status  =  'Processing'
            """,  new { commandId,  workerId,  error,  retryable,  status },  cancellationToken: cancellationToken));
    }

    public Task RejectAsync(Guid commandId,  string workerId,  string reason,  CancellationToken cancellationToken)
         =>  UpdateTerminalAsync(commandId,  workerId,  "Rejected",  reason,  cancellationToken);

    private async Task UpdateTerminalAsync(Guid commandId,  string workerId,  string status,  string? error,  CancellationToken cancellationToken)
    {
        await using var connection  =  await factory.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            """
            update trading_dashboard.bot_commands
            set status  =  @status,  completed_at_utc  =  now(),  error  =  @error,  processing_worker_id  =  null
            where command_id  =  @commandId and processing_worker_id  =  @workerId and status  =  'Processing'
            """,  new { commandId,  workerId,  status,  error },  cancellationToken: cancellationToken));
    }

    private sealed record CommandRow(Guid CommandId,  string BotName,  string Command,  string Status,  string RequestedBy,  string Reason,  string Payload,  DateTime RequestedAtUtc,  int AttemptCount);
}
