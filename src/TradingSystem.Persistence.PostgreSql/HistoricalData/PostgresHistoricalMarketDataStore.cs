using Dapper;
using TradingSystem.Domain.MarketData;
using TradingSystem.Persistence.PostgreSql.Connections;
using TradingSystem.JobOrchestration.Models;
using TradingSystem.JobOrchestration.Contracts;
using TradingSystem.Backtesting.Bots.Models;

namespace TradingSystem.Persistence.PostgreSql.HistoricalData;

public sealed class PostgresHistoricalMarketDataStore(
    ITradingDbConnectionFactory factory) 
    : IHistoricalMarketDataStore, IHistoricalSignalStore
{
    public async Task<IReadOnlyList<MarketCandle>> LoadCandlesAsync(string symbol, string interval, DateTime from, DateTime to, CancellationToken ct) 
    { 
        const string sql = "select symbol, " +
            "interval, " +
            "open_time_utc OpenTimeUtc, " +
            "open_time_utc + trading_dashboard.interval_duration(interval)-interval '1 millisecond' CloseTimeUtc, " +
            "open Open, " +
            "high High, " +
            "low Low, " +
            "close Close, " +
            "volume Volume, " +
            "true IsClosed" +
            " from trading_dashboard.market_candles " +
            "where symbol = @symbol " +
            "and interval = @interval " +
            "and open_time_utc >= @from " +
            "and open_time_utc < @to " +
            "order by open_time_utc"; 
        
        await using var connection = await factory.OpenAsync(ct); 
        
        return (await connection.QueryAsync<MarketCandle>(
            new CommandDefinition(
                sql, 
                new 
                { symbol = symbol.ToUpperInvariant(), interval = interval.ToLowerInvariant(), from, to },
                cancellationToken: ct)))
                .AsList(); 
    }
    
    public async Task UpsertCandlesAsync(IReadOnlyCollection<MarketCandle> candles, CancellationToken ct) 
    { 
        if (candles.Count == 0) 
            return; 
        
        const string sql = "insert into trading_dashboard.market_candles" +
            "(symbol, " +
            "interval, " +
            "open_time_utc, " +
            "open, " +
            "high, " +
            "low, " +
            "close, " +
            "volume) " +
            "values(@Symbol, @Interval, @OpenTimeUtc, @Open, @High, @Low, @Close, @Volume) " +
            "on conflict(symbol, interval, open_time_utc) " +
            "do update set open = excluded.open, " +
                "high = excluded.high, " +
                "low = excluded.low, " +
                "close = excluded.close, " +
                "volume = excluded.volume"; 
        
        await using var connection = await factory.OpenAsync(ct);
        
        await connection.ExecuteAsync(new CommandDefinition(sql, candles, cancellationToken: ct));
    }
    
    public async Task ReplaceGapsAsync(string symbol, string interval, IReadOnlyCollection<HistoricalDataGap> gaps, CancellationToken ct) 
    { 
        await using var connection = await factory.OpenAsync(ct);   
        await using var transaction = await connection.BeginTransactionAsync(ct); 
        
        await connection.ExecuteAsync(
            new CommandDefinition("delete from trading_dashboard.historical_data_gaps " +
            "where symbol = @symbol and interval = @interval",
            new { symbol, interval }, 
            transaction, 
            cancellationToken: ct)); 
        
        if (gaps.Count > 0) 
            await connection.ExecuteAsync(
                new CommandDefinition("insert into trading_dashboard.historical_data_gaps" +
                "(symbol, " +
                "interval, " +
                "gap_from_utc, " +
                "gap_to_utc," +
                " missing_candles) " +
                "values(@Symbol, @Interval, @FromUtc, @ToUtc, @MissingCandles)", 
                gaps, 
                transaction, 
                cancellationToken: ct)); 
        
        await transaction.CommitAsync(ct); 
    }
    
    public async Task<bool> HasGapsAsync(string symbol, string interval, DateTime from, DateTime to, CancellationToken ct) 
    { 
        await using var connection = await factory.OpenAsync(ct); 
        
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                "select exists" +
                    "(select 1 " +
                    "from trading_dashboard.historical_data_gaps " +
                    "where symbol = @symbol " +
                    "and interval = @interval " +
                    "and gap_from_utc <= @to " +
                    "and gap_to_utc >= @from)", 
                new { symbol, interval, from, to }, 
                cancellationToken: ct)); 
    }
    
    public async Task<DateTime?> GetLatestOpenTimeAsync(string symbol, string interval, CancellationToken ct) 
    { 
        await using var connection = await factory.OpenAsync(ct); 
        
        return await connection.ExecuteScalarAsync<DateTime?>(
            new CommandDefinition(
                "select max(open_time_utc) " +
                "from trading_dashboard.market_candles " +
                "where symbol = @symbol" +
                " and interval = @interval", 
            new { symbol, interval }, 
            cancellationToken: ct));
    }
    
    public async Task<IReadOnlyList<HistoricalBotSignal>> LoadAsync(string bot, string symbol, DateTime from, DateTime to, CancellationToken ct) 
    { 
        const string sql = "select " +
            "signal_time_utc TimeUtc, " +
            "case when lower(side) = 'long' " +
               "then 0 else 1 end Side, " +
            "source Source, " +
            "signal_id SignalId " +
            "from trading_history.signals " +
            "where bot_name = @bot " +
            "and symbol = @symbol " +
            "and signal_time_utc >= @from " +
            "and signal_time_utc < @to " +
            "order by signal_time_utc"; 
        
        await using var command = await factory.OpenAsync(ct); 
        
        return (await command.QueryAsync<HistoricalBotSignal>(new CommandDefinition(sql, new { bot, symbol, from, to }, cancellationToken: ct))).AsList(); 
    }
}
