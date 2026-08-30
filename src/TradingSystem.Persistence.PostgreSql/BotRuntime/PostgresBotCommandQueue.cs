using Dapper;
using System.Text.Json;
using TradingSystem.BotRuntime.Commands;
using TradingSystem.BotRuntime.Commands.Models;
using TradingSystem.BotRuntime.Commands.Models.Enums;
using TradingSystem.Persistence.PostgreSql.Connections;

namespace TradingSystem.Persistence.PostgreSql.BotRuntime;

public sealed class PostgresBotCommandQueue(
    ITradingDbConnectionFactory factory)
    : IBotCommandQueue
{
    public async Task<IReadOnlyCollection<BotCommand>> ClaimPendingAsync(string workerId, int batchSize, TimeSpan processingTimeout, CancellationToken ct)
    {
        await using var connection = await factory.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        
        var rows = (await connection.QueryAsync<CommandRow>(new CommandDefinition(
            """
            with candidates as (
                select 
                    command_id
                from trading_dashboard.bot_commands
                where (status = 'Pending' 
                    and next_attempt_at_utc <= now())
                    or (status  =  'Processing' and processing_started_at_utc < now() - @processingTimeout)
                order by requested_at_utc
                for update skip locked
                limit @batchSize
            )
            update trading_dashboard.bot_commands c
            set 
                status = 'Processing',  
                processing_started_at_utc = now(),  
                processing_worker_id = @workerId, 
                attempt_count = attempt_count + 1
            from candidates
            where c.command_id = candidates.command_id
            returning 
                c.command_id CommandId,
                c.bot_name BotName, 
                c.command Command, 
                c.status Status,  
                c.requested_by RequestedBy,  
                c.reason Reason, 
                c.payload::text Payload,  
                c.requested_at_utc RequestedAtUtc, 
                c.attempt_count AttemptCount
            """, 
            new 
            { 
                workerId, 
                batchSize = Math.Clamp(batchSize, 1, 100), 
                processingTimeout 
            }, 
            transaction, 
            cancellationToken: ct)))
            .AsList();
       
        await transaction.CommitAsync(ct);
        
        return [..rows.Select(x => 
            new BotCommand(
                x.CommandId, 
                x.BotName, 
                Enum.Parse<BotCommandType>(x.Command, true), 
                Enum.Parse<BotCommandStatus>(x.Status, true), 
                x.RequestedBy, 
                x.Reason, 
                JsonDocument.Parse(x.Payload), 
                x.RequestedAtUtc, 
                x.AttemptCount))];
    }

    public Task CompleteAsync(Guid commandId, string workerId, CancellationToken ct)
         => UpdateTerminalAsync(commandId, workerId, "Completed", null, ct);

    public async Task FailAsync(Guid commandId, string workerId, string error, bool retryable, CancellationToken ct)
    {
        await using var connection = await factory.OpenAsync(ct);
        
        var status = retryable ? "Pending" : "Failed";
        
        await connection.ExecuteAsync(new CommandDefinition(
            """
            update trading_dashboard.bot_commands
            set 
                status = @status,
                error = @error, 
                next_attempt_at_utc = 
                    case 
                        when @retryable then now() + make_interval(secs  =>  least(300,  power(2,  greatest(attempt_count, 1))::int)) 
                        else next_attempt_at_utc 
                    end, 
                completed_at_utc = 
                    case 
                        when @retryable  then null 
                        else now() 
                    end,
                completed_by_worker_id = 
                    case 
                        when @retryable then completed_by_worker_id 
                        else @workerId 
                    end,
                processing_worker_id = null
            where command_id = @commandId 
                and processing_worker_id = @workerId 
                and status = 'Processing'
            """
            , 
            new { commandId, workerId, error, retryable, status }, 
            cancellationToken: ct));
    }

    public Task RejectAsync(Guid commandId, string workerId, string reason, CancellationToken ct)
         => UpdateTerminalAsync(commandId, workerId, "Rejected", reason, ct);

    private async Task UpdateTerminalAsync(Guid commandId, string workerId, string status, string? error, CancellationToken ct)
    {
        await using var connection = await factory.OpenAsync(ct);
        
        await connection.ExecuteAsync(new CommandDefinition(
            """
            update trading_dashboard.bot_commands
            set 
                status = @status,
                completed_at_utc = now(),
                error = @error,
                completed_by_worker_id = @workerId,
                processing_worker_id = null
            where command_id = @commandId
                and processing_worker_id = @workerId 
                and status = 'Processing'
            """,
            new { commandId, workerId, status, error }, 
            cancellationToken: ct));
    }

    private sealed record CommandRow(Guid CommandId, string BotName, string Command, string Status, string RequestedBy, string Reason, string Payload, DateTime RequestedAtUtc, int AttemptCount);
}

