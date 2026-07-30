using TradingSystem.Domain.Enums;

namespace TradingSystem.PortfolioManagement;

public sealed record PaperPortfolioPosition(
    string BotName,
    string ShortId,
    string Symbol,
    PositionSide Side,
    decimal Quantity,
    decimal EntryPrice,
    DateTime OpenedAtUtc);

