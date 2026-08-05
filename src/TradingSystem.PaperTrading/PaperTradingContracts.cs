using TradingSystem.Domain.Enums;

namespace TradingSystem.PaperTrading;

public enum PaperOrderStatus
{
    Open = 1,
    Filled = 2,
    Cancelled = 3,
    Rejected = 4
}

public enum PaperPositionStatus
{
    Open = 1,
    Closed = 2
}

public sealed class PaperTradingPosition
{
    public Guid PositionId { get; set; }
    public string ShortId { get; set; } = string.Empty;
    public string? SignalId { get; set; }
    public string StrategyVersion { get; set; } = "unknown";
    public string BotName { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public PositionSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal TakeProfitPrice { get; set; }
    public decimal StopLossPrice { get; set; }
    public decimal EntryFee { get; set; }
    public decimal? ExitPrice { get; set; }
    public decimal? ExitFee { get; set; }
    public decimal? RealizedPnl { get; set; }
    public PaperPositionStatus Status { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime OpenedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public string? CloseReason { get; set; }
    public long Version { get; set; }
}

public sealed record PaperTradingAccount(
    decimal InitialBalance,
    decimal RealizedPnl,
    decimal Fees,
    decimal Equity,
    int OpenPositions,
    int ClosedPositions,
    DateTime CalculatedAtUtc);

public interface IPaperTradingStore
{
    Task CreateAsync(PaperTradingPosition position, CancellationToken ct);
    Task<PaperTradingPosition?> GetAsync(string botName, string shortId, CancellationToken ct);
    Task<IReadOnlyCollection<PaperTradingPosition>> GetOpenAsync(CancellationToken ct);
    Task<IReadOnlyCollection<PaperTradingPosition>> QueryAsync(string? botName, string? symbol, PaperPositionStatus? status, int skip, int take, CancellationToken ct);
    Task<bool> TryCloseAsync(Guid positionId, long expectedVersion, decimal exitPrice, decimal exitFee, decimal realizedPnl, string reason, DateTime closedAtUtc, CancellationToken ct);
    Task<PaperTradingAccount> GetAccountAsync(decimal initialBalance, CancellationToken ct);
    Task ResetAsync(string actor, CancellationToken ct);
}
