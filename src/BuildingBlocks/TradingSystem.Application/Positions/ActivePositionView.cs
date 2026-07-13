using TradingSystem.Domain.Enums;

namespace TradingSystem.Application.Positions;

public sealed record ActivePositionView
{
    public required string ShortId { get; init; }

    public required string BotName { get; init; }

    public required string Symbol { get; init; }

    public required PositionSide Side { get; init; }

    public decimal EntryPrice { get; init; }

    public decimal Quantity { get; init; }

    public decimal RemainingQuantity { get; init; }

    public decimal? TpPrice { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}