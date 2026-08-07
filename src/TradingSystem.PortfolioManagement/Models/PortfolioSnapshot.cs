namespace TradingSystem.PortfolioManagement.Models;

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
