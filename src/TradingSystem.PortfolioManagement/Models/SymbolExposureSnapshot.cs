namespace TradingSystem.PortfolioManagement.Models;

public sealed record SymbolExposureSnapshot(
    string Symbol,
    int OpenPositions,
    decimal LongNotional,
    decimal ShortNotional,
    decimal GrossNotional,
    decimal NetNotional,
    decimal UnrealizedPnl);
