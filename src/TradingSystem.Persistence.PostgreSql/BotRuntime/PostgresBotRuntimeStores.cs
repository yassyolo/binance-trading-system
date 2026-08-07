using System.Data;
using Dapper;
using TradingSystem.BotRuntime.Runtime.Contracts;
using TradingSystem.BotRuntime.Runtime.Models;
using TradingSystem.BotRuntime.Runtime.Models.Enums;
using TradingSystem.Persistence.PostgreSql.Connections;

namespace TradingSystem.Persistence.PostgreSql.BotRuntime;

public sealed class PostgresBotRuntimeStateStore(
    ITradingDbConnectionFactory factory) 
    : IBotRuntimeStateStore
{
	public async Task<BotRuntimeState?> GetAsync(string botName, CancellationToken ct)
	{
		await using var connection = await factory.OpenAsync(ct);
		return await connection.QuerySingleOrDefaultAsync<BotRuntimeState>(new CommandDefinition(
			"""
            select bot_name BotName,  runtime_status Status,  runtime_version Version, 
                   runtime_updated_at_utc UpdatedAtUtc,  runtime_updated_by UpdatedBy, 
                   runtime_reason Reason,  execution_enabled ExecutionEnabled
            from trading_dashboard.bot_configurations
            where bot_name  =  @botName
            """, new { botName }, cancellationToken: ct));
	}

	public async Task<IReadOnlyCollection<BotRuntimeState>> GetAllAsync(CancellationToken ct)
	{
		await using var connection = await factory.OpenAsync(ct);
		return (await connection.QueryAsync<BotRuntimeState>(new CommandDefinition(
			"""
            select bot_name BotName,  runtime_status Status,  runtime_version Version, 
                   runtime_updated_at_utc UpdatedAtUtc,  runtime_updated_by UpdatedBy, 
                   runtime_reason Reason,  execution_enabled ExecutionEnabled
            from trading_dashboard.bot_configurations
            order by bot_name
            """, cancellationToken: ct))).AsList();
	}

	public async Task<BotRuntimeState> TransitionAsync(string botName, BotRuntimeStatus status, long expectedVersion, string user, string reason, bool executionEnabled, CancellationToken ct)
	{
		await using var connection = await factory.OpenAsync(ct);
		var updated = await connection.QuerySingleOrDefaultAsync<BotRuntimeState>(new CommandDefinition(
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
            """, new { botName, status = status.ToString(), expectedVersion, user, reason, executionEnabled }, cancellationToken: ct));
		return updated ?? throw new DBConcurrencyException($"Runtime state for '{botName}' was changed concurrently.");
	}
}
