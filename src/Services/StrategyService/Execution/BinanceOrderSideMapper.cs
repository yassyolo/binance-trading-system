using TradingSystem.Domain.Enums;

namespace StrategyService.Execution;

public static class BinanceOrderSideMapper
{
    public static string ToEntrySide(PositionSide side)
        => side == PositionSide.Long ? "BUY" : "SELL";

    public static string ToCloseSide(PositionSide side)
        => side == PositionSide.Long ? "SELL" : "BUY";

    public static string ToPositionSide(PositionSide side)
        => side == PositionSide.Long ? "LONG" : "SHORT";
}
