namespace TradingSystem.PortfolioManagement.Models;

public sealed record BotExposureSnapshot(
    string BotName,
    int OpenPositions,
    decimal GrossNotional,
    decimal NetNotional,
    decimal UnrealizedPnl,
    decimal EstimatedInitialMargin);
