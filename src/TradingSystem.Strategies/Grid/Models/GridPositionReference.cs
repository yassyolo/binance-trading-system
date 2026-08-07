using TradingSystem.Domain.Enums;

namespace TradingSystem.Strategies.Grid.Models;

public sealed record GridPositionReference(
    PositionSide Side,
    decimal TakeProfitPrice,
    DateTime OpenedAtUtc);
