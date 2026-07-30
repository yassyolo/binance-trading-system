using TradingSystem.Domain.Enums;

namespace TradingSystem.PortfolioManagement;

public sealed record PortfolioPositionSnapshot(
    string BotName,
    string PositionId,
    string Symbol,
    PositionSide Side,
    decimal Quantity,
    decimal EntryPrice,
    decimal MarkPrice,
    decimal Notional,
    decimal SignedNotional,
    decimal UnrealizedPnl,
    decimal EstimatedInitialMargin,
    DateTime OpenedAtUtc);

public sealed record SymbolExposureSnapshot(
    string Symbol,
    int OpenPositions,
    decimal LongNotional,
    decimal ShortNotional,
    decimal GrossNotional,
    decimal NetNotional,
    decimal UnrealizedPnl);

public sealed record BotExposureSnapshot(
    string BotName,
    int OpenPositions,
    decimal GrossNotional,
    decimal NetNotional,
    decimal UnrealizedPnl,
    decimal EstimatedInitialMargin);

public sealed record PortfolioPerformanceSnapshot(
    decimal RealizedPnlToday,
    decimal PeakEquityToday,
    int ConsecutiveLosses,
    DateTime AsOfUtc)
{
    public static PortfolioPerformanceSnapshot Empty(DateTime asOfUtc, decimal currentEquity) =>
        new(0m, currentEquity, 0, asOfUtc);
}

public sealed record PortfolioSnapshot(
    DateTime GeneratedAtUtc,
    decimal StartingEquity,
    decimal RealizedPnlToday,
    decimal UnrealizedPnl,
    decimal Equity,
    decimal PeakEquityToday,
    decimal DailyDrawdown,
    decimal DailyDrawdownPercent,
    int ConsecutiveLosses,
    int OpenPositions,
    decimal GrossNotional,
    decimal NetNotional,
    decimal EstimatedInitialMargin,
    IReadOnlyCollection<PortfolioPositionSnapshot> Positions,
    IReadOnlyCollection<SymbolExposureSnapshot> Symbols,
    IReadOnlyCollection<BotExposureSnapshot> Bots);