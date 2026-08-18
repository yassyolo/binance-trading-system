using TradingSystem.Domain.Enums;

namespace TradingSystem.PortfolioManagement.Models;

public sealed record PaperPortfolioPosition(
    string BotName,
    string ShortId,
    string Symbol,
    PositionSide Side,
    decimal Quantity,
    decimal EntryPrice,
    DateTime OpenedAtUtc);