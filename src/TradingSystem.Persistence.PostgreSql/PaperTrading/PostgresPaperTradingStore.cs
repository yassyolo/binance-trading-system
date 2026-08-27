using Dapper;
using TradingSystem.PaperTrading.Contracts;
using TradingSystem.PaperTrading.Models;
using TradingSystem.PaperTrading.Models.Enums;
using TradingSystem.Persistence.PostgreSql.Connections;
using TradingSystem.PortfolioManagement.Models;
using TradingSystem.PortfolioManagement.Position;

namespace TradingSystem.Persistence.PostgreSql.PaperTrading;

public sealed class PostgresPaperTradingStore(
    ITradingDbConnectionFactory connections)
    : IPaperTradingStore,
      IPaperPortfolioPositionSource
{
    public async Task CreateAsync(PaperTradingPosition position, CancellationToken ct)
    {
        const string sql = """
            insert into trading_paper.positions
                (position_id, short_id, signal_id, strategy_version, bot_name, symbol, side,
                 quantity, entry_price, take_profit_price, stop_loss_price, entry_fee,
                 status, source, opened_at_utc, version)
            values
                (@PositionId, @ShortId, @SignalId, @StrategyVersion, @BotName, @Symbol, @Side,
                 @Quantity, @EntryPrice, @TakeProfitPrice, @StopLossPrice, @EntryFee,
                 @Status, @Source, @OpenedAtUtc, @Version);
            """;

        await using var connection = await connections.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            Map(position),
            commandTimeout: connections.CommandTimeoutSeconds,
            cancellationToken: ct));
    }

    public async Task<PaperTradingPosition?> GetAsync(string botName, string shortId, CancellationToken ct)
    {
        await using var connection = await connections.OpenAsync(ct);
        
        return await connection.QuerySingleOrDefaultAsync<PaperTradingPosition>(
            new CommandDefinition(
                BaseSelect + " and bot_name = @botName and short_id = @shortId",
                new { botName, shortId },
                commandTimeout: connections.CommandTimeoutSeconds,
                cancellationToken: ct));
    }

    public async Task<IReadOnlyCollection<PaperTradingPosition>> GetOpenAsync(CancellationToken ct)
    {
        await using var connection = await connections.OpenAsync(ct);
        
        var rows = await connection.QueryAsync<PaperTradingPosition>(
            new CommandDefinition(
                BaseSelect + """
                     and status = @Status
                     order by opened_at_utc
                    """,
                new { Status = (short)PaperPositionStatus.Open },
                commandTimeout: connections.CommandTimeoutSeconds,
                cancellationToken: ct));

        return rows.AsList();
    }

    async Task<IReadOnlyCollection<PaperPortfolioPosition>> IPaperPortfolioPositionSource.GetOpenAsync(CancellationToken ct)
    {
        var positions = await GetOpenAsync(ct);
        return positions.Select(position => new PaperPortfolioPosition(
            position.BotName,
            position.ShortId,
            position.Symbol,
            position.Side,
            position.Quantity,
            position.EntryPrice,
            position.OpenedAtUtc)).ToArray();
    }

    public async Task<IReadOnlyCollection<PaperTradingPosition>> QueryAsync(
        string? botName,
        string? symbol,
        PaperPositionStatus? status,
        int skip,
        int take,
        CancellationToken ct)
    {
        const string where = """
             and (@botName is null or bot_name = @botName)
             and (@symbol is null or symbol = @symbol)
             and (@status is null or status = @status)
             order by opened_at_utc desc
             offset @skip limit @take
            """;

        await using var connection = await connections.OpenAsync(ct);
        var rows = await connection.QueryAsync<PaperTradingPosition>(
            new CommandDefinition(
                BaseSelect + where,
                new
                {
                    botName,
                    symbol = string.IsNullOrWhiteSpace(symbol) ? null : symbol.ToUpperInvariant(),
                    status = (int?)status,
                    skip = Math.Max(0, skip),
                    take = Math.Clamp(take, 1, 1000)
                },
                commandTimeout: connections.CommandTimeoutSeconds,
                cancellationToken: ct));

        return rows.AsList();
    }

    public async Task<bool> TryCloseAsync(
        Guid id,
        long expectedVersion,
        decimal exitPrice,
        decimal exitFee,
        decimal pnl,
        string reason,
        DateTime closedAt,
        CancellationToken ct)
    {
        const string sql = """
            update trading_paper.positions
            set 
                exit_price = @exitPrice,
                exit_fee = @exitFee,
                realized_pnl = @pnl,
                status = 2,
                closed_at_utc = @closedAt,
                close_reason = @reason,
                version = version + 1
            where position_id = @id
              and version = @expectedVersion
              and status = 1;
            """;

        await using var connection = await connections.OpenAsync(ct);
        
        return await connection.ExecuteAsync(
            new CommandDefinition(
            sql,
            new 
            { 
                id,
                expectedVersion, 
                exitPrice, 
                exitFee, 
                pnl, 
                reason, 
                closedAt 
            },
            commandTimeout: connections.CommandTimeoutSeconds,
            cancellationToken: ct)) == 1;
    }

    public async Task<PaperTradingAccount> GetAccountAsync(decimal initialBalance, CancellationToken ct)
    {
        const string sql = """
            select coalesce(sum(realized_pnl), 0) RealizedPnl,
                   coalesce(sum(entry_fee + coalesce(exit_fee, 0)), 0) Fees,
                   count(*) filter(where status = 1)::int OpenPositions,
                   count(*) filter(where status = 2)::int ClosedPositions
            from trading_paper.positions
            where archived = false;
            """;

        await using var connection = await connections.OpenAsync(ct);
        var row = await connection.QuerySingleAsync<AccountRow>(new CommandDefinition(
            sql,
            commandTimeout: connections.CommandTimeoutSeconds,
            cancellationToken: ct));

        return new PaperTradingAccount(
            initialBalance,
            row.RealizedPnl,
            row.Fees,
            initialBalance + row.RealizedPnl,
            row.OpenPositions,
            row.ClosedPositions,
            DateTime.UtcNow);
    }

    public async Task ResetAsync(string actor, CancellationToken ct)
    {
        const string sql = """
            insert into trading_paper.reset_events(actor, reset_at_utc, positions_archived)
            select @actor, now(), count(*)
            from trading_paper.positions
            where archived = false;

            update trading_paper.positions
            set archived = true
            where archived = false;
            """;

        await using var connection = await connections.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { actor },
            transaction,
            commandTimeout: connections.CommandTimeoutSeconds,
            cancellationToken: ct));
        await transaction.CommitAsync(ct);
    }

    private const string BaseSelect = """
        select position_id PositionId,
               short_id ShortId,
               signal_id SignalId,
               strategy_version StrategyVersion,
               bot_name BotName,
               symbol Symbol,
               side Side,
               quantity Quantity,
               entry_price EntryPrice,
               take_profit_price TakeProfitPrice,
               stop_loss_price StopLossPrice,
               entry_fee EntryFee,
               exit_price ExitPrice,
               exit_fee ExitFee,
               realized_pnl RealizedPnl,
               status Status,
               source Source,
               opened_at_utc OpenedAtUtc,
               closed_at_utc ClosedAtUtc,
               close_reason CloseReason,
               version Version
        from trading_paper.positions
        where archived = false
        """;

    private static object Map(PaperTradingPosition position) => new
    {
        position.PositionId,
        position.ShortId,
        position.SignalId,
        position.StrategyVersion,
        position.BotName,
        Symbol = position.Symbol.ToUpperInvariant(),
        Side = (int)position.Side,
        position.Quantity,
        position.EntryPrice,
        position.TakeProfitPrice,
        position.StopLossPrice,
        position.EntryFee,
        Status = (int)position.Status,
        position.Source,
        position.OpenedAtUtc,
        position.Version
    };

    private sealed record AccountRow(decimal RealizedPnl, decimal Fees, int OpenPositions, int ClosedPositions);
}
