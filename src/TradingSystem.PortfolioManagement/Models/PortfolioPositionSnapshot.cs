using TradingSystem.Domain.Enums;

namespace TradingSystem.PortfolioManagement.Models;

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
