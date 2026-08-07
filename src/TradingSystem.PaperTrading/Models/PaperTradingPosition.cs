using TradingSystem.Domain.Enums;
using TradingSystem.PaperTrading.Models.Enums;

namespace TradingSystem.PaperTrading.Models;

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
