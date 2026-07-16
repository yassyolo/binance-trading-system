namespace TradingSystem.Backtesting.Bot8012.Models;

public enum BacktestSide { Long, Short }
public sealed record Candle(DateTime OpenTimeUtc, DateTime CloseTimeUtc, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);
public sealed record HistoricalSignal(DateTime TimeUtc, BacktestSide Side, string Source, string? SignalId = null);

public sealed record Bot8012BacktestOptions
{
    public string BotName { get; init; } = "BOT8012";
    public string StrategyVersion { get; init; } = "1.0.0";
    public string Symbol { get; init; } = "BTCUSDC";
    public decimal InitialBalance { get; init; } = 10_000m;
    public decimal Quantity { get; init; } = 0.002m;
    public int Leverage { get; init; } = 50;
    public decimal PriceDistance { get; init; } = 400m;
    public decimal ProfitDistance { get; init; } = 200m;
    public int OrderSideLimit { get; init; } = 2;
    public int CooldownSeconds { get; init; } = 180;
    public bool EnableLong { get; init; } = true;
    public bool EnableShort { get; init; } = true;
    public decimal EntryFeeRate { get; init; } = 0.0005m;
    public decimal ExitFeeRate { get; init; } = 0.0005m;
    public decimal SlippageBasisPoints { get; init; } = 1m;
    public bool ForceCloseAtEnd { get; init; } = true;
}

public sealed record SimulatedPosition
{
    public required string Id { get; init; }
    public required BacktestSide Side { get; init; }
    public required DateTime SignalTimeUtc { get; init; }
    public required DateTime EntryTimeUtc { get; init; }
    public required decimal EntryPrice { get; init; }
    public required decimal TpPrice { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal EntryFee { get; init; }
    public DateTime? ExitTimeUtc { get; set; }
    public decimal? ExitPrice { get; set; }
    public decimal ExitFee { get; set; }
    public decimal GrossPnl { get; set; }
    public string? ExitReason { get; set; }
    public decimal NetPnl => GrossPnl - EntryFee - ExitFee;
    public bool Closed => ExitTimeUtc is not null;
}

public sealed record SignalDecision(DateTime TimeUtc, BacktestSide Side, string Decision, string Reason, decimal MarkPrice);
public sealed record EquityPoint(DateTime TimeUtc, decimal Balance, decimal Equity, decimal DrawdownPercent);
public sealed record BacktestMetrics(int Signals, int Opened, int Blocked, int Closed, int Winning, int Losing, decimal WinRate, decimal NetProfit, decimal TotalFees, decimal ProfitFactor, decimal MaxDrawdownPercent, decimal EndingBalance);
public sealed record Bot8012BacktestResult(Bot8012BacktestOptions Options, IReadOnlyList<SimulatedPosition> Positions, IReadOnlyList<SignalDecision> Decisions, IReadOnlyList<EquityPoint> Equity, BacktestMetrics Metrics);
