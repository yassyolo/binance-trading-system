using Dapper;
using TradingSystem.BotRuntime.Configuration;
using TradingSystem.BotRuntime.Configuration.Contracts;
using TradingSystem.Persistence.PostgreSql.Connections;

namespace TradingSystem.Persistence.PostgreSql.BotRuntime;

public sealed class PostgresBotRuntimeConfigurationStore(
    ITradingDbConnectionFactory factory) 
    : IBotRuntimeConfigurationStore
{
    private const string Projection = """
        select 
            bot_name BotName,  
            strategy_type StrategyType,  
            symbol Symbol,  
            environment Environment, 
            signal_source SignalSource, 
            enable_long EnableLong,  
            enable_short EnableShort, 
            quantity Quantity,  
            leverage Leverage,  
            price_distance PriceDistance, 
            profit_distance ProfitDistance,  
            order_side_limit OrderSideLimit, 
            cooldown_seconds CooldownSeconds,  
            version Version,  
            updated_at_utc UpdatedAtUtc, 
            restart_required RestartRequired
        from trading_dashboard.bot_configurations
        """;

    public async Task<BotRuntimeConfiguration?> GetAsync(string botName, CancellationToken ct)
    {
        await using var connection = await factory.OpenAsync(ct);
       
        return await connection.QuerySingleOrDefaultAsync<BotRuntimeConfiguration>(
            new CommandDefinition(
            Projection + " where bot_name = @botName",
            new { botName }, 
            cancellationToken: 
            ct));
    }

    public async Task<IReadOnlyCollection<BotRuntimeConfiguration>> GetChangedSinceAsync(DateTime changedSinceUtc, CancellationToken ct)
    {
        await using var connection = await factory.OpenAsync(ct);
       
        return (await connection.QueryAsync<BotRuntimeConfiguration>(
            new CommandDefinition(
            Projection + " where updated_at_utc > @changedSinceUtc order by updated_at_utc", 
            new { changedSinceUtc }, 
            cancellationToken: ct)))
            .AsList();
    }
}