using System.Text.Json;
using Dapper;
using TradingSystem.Domain.Enums;
using TradingSystem.PaperTrading;
using TradingSystem.Persistence.PostgreSql.Connections;

namespace TradingSystem.Persistence.PostgreSql.PaperTrading;

public sealed class PostgresPaperTradingStore(ITradingDbConnectionFactory connections) : IPaperTradingStore
{
    public async Task CreateAsync(PaperTradingPosition p,  CancellationToken ct)
    {
        const string sql  =  """
        INSERT INTO trading_paper.positions
        (position_id, short_id, bot_name, symbol, side, quantity, entry_price, take_profit_price, stop_loss_price, entry_fee, status, source, opened_at_utc, version)
        VALUES (@PositionId, @ShortId, @BotName, @Symbol, @Side, @Quantity, @EntryPrice, @TakeProfitPrice, @StopLossPrice, @EntryFee, @Status, @Source, @OpenedAtUtc, @Version);
        """;
        await using var c = await connections.OpenAsync(ct); await c.ExecuteAsync(new CommandDefinition(sql,  Map(p),  cancellationToken:ct));
    }

    public async Task<PaperTradingPosition?> GetAsync(string botName, string shortId, CancellationToken ct)
    { await using var c = await connections.OpenAsync(ct); return await c.QuerySingleOrDefaultAsync<PaperTradingPosition>(new CommandDefinition(BaseSelect+" AND bot_name = @botName AND short_id = @shortId", new{botName, shortId}, cancellationToken:ct)); }

    public async Task<IReadOnlyCollection<PaperTradingPosition>> GetOpenAsync(CancellationToken ct)
    { await using var c = await connections.OpenAsync(ct); var x = await c.QueryAsync<PaperTradingPosition>(new CommandDefinition(BaseSelect+" AND status = 1 ORDER BY opened_at_utc", cancellationToken:ct)); return x.AsList(); }

    public async Task<IReadOnlyCollection<PaperTradingPosition>> QueryAsync(string? botName, string? symbol, PaperPositionStatus? status, int skip, int take, CancellationToken ct)
    { const string where = " WHERE (@botName IS NULL OR bot_name = @botName) AND (@symbol IS NULL OR symbol = @symbol) AND (@status IS NULL OR status = @status) ORDER BY opened_at_utc DESC OFFSET @skip LIMIT @take"; await using var c = await connections.OpenAsync(ct); var x = await c.QueryAsync<PaperTradingPosition>(new CommandDefinition(BaseSelect+where, new{botName, symbol = status is null?symbol:symbol?.ToUpperInvariant(), status = (int?)status, skip, take}, cancellationToken:ct)); return x.AsList(); }

    public async Task<bool> TryCloseAsync(Guid id, long expectedVersion, decimal exitPrice, decimal exitFee, decimal pnl, string reason, DateTime closedAt, CancellationToken ct)
    { const string sql = """UPDATE trading_paper.positions SET exit_price = @exitPrice, exit_fee = @exitFee, realized_pnl = @pnl, status = 2, closed_at_utc = @closedAt, close_reason = @reason, version = version+1 WHERE position_id = @id AND version = @expectedVersion AND status = 1"""; await using var c = await connections.OpenAsync(ct); return await c.ExecuteAsync(new CommandDefinition(sql, new{id, expectedVersion, exitPrice, exitFee, pnl, reason, closedAt}, cancellationToken:ct))==1; }

    public async Task<PaperTradingAccount> GetAccountAsync(decimal initialBalance, CancellationToken ct)
    { const string sql = """SELECT COALESCE(SUM(realized_pnl), 0) RealizedPnl, COALESCE(SUM(entry_fee+COALESCE(exit_fee, 0)), 0) Fees, COUNT(*) FILTER(WHERE status = 1) OpenPositions, COUNT(*) FILTER(WHERE status = 2) ClosedPositions FROM trading_paper.positions WHERE archived = false"""; await using var c = await connections.OpenAsync(ct); var x = await c.QuerySingleAsync<AccountRow>(new CommandDefinition(sql, cancellationToken:ct)); return new(initialBalance, x.RealizedPnl, x.Fees, initialBalance+x.RealizedPnl, x.OpenPositions, x.ClosedPositions, DateTime.UtcNow); }

    public async Task ResetAsync(string actor, CancellationToken ct)
    { await using var c = await connections.OpenAsync(ct); await c.ExecuteAsync(new CommandDefinition("INSERT INTO trading_paper.reset_events(actor, reset_at_utc, positions_archived) SELECT @actor, now(), COUNT(*) FROM trading_paper.positions; UPDATE trading_paper.positions SET archived = true WHERE archived = false;", new{actor}, cancellationToken:ct)); }

    private const string BaseSelect = """SELECT position_id PositionId, short_id ShortId, bot_name BotName, symbol Symbol, side Side, quantity Quantity, entry_price EntryPrice, take_profit_price TakeProfitPrice, stop_loss_price StopLossPrice, entry_fee EntryFee, exit_price ExitPrice, exit_fee ExitFee, realized_pnl RealizedPnl, status Status, source Source, opened_at_utc OpenedAtUtc, closed_at_utc ClosedAtUtc, close_reason CloseReason, version Version FROM trading_paper.positions WHERE archived = false""";
    private static object Map(PaperTradingPosition p) => new{p.PositionId, p.ShortId, p.BotName, p.Symbol, Side = (int)p.Side, p.Quantity, p.EntryPrice, p.TakeProfitPrice, p.StopLossPrice, p.EntryFee, Status = (int)p.Status, p.Source, p.OpenedAtUtc, p.Version};
    private sealed record AccountRow(decimal RealizedPnl, decimal Fees, int OpenPositions, int ClosedPositions);
}
