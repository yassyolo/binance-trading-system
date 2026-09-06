using TradingSystem.Contracts.Klines;

namespace StrategyService.Signals.Models;

public sealed class JoinedState
{
    public ClosedKlineMessage? Candle { get; set; }
    public long CandleCloseTime { get; set; }
    public Dictionary<string, decimal> Indicators { get; } = new(StringComparer.OrdinalIgnoreCase);
    public SemaphoreSlim Gate { get; } = new(1, 1);
}