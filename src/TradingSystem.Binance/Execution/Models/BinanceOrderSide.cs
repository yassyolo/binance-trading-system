using TradingSystem.Domain.Enums;

namespace TradingSystem.Binance.Execution.Models;

public static class BinanceOrderSide
{
    public static string Entry(PositionSide side) => side == PositionSide.Long ? "BUY" : "SELL";
    public static string Close(PositionSide side) => side == PositionSide.Long ? "SELL" : "BUY";
    public static string Position(PositionSide side) => side == PositionSide.Long ? "LONG" : "SHORT";
    public static bool TryParsePosition(string? value, out PositionSide side)
    {
        if (value?.Equals("LONG", StringComparison.OrdinalIgnoreCase) == true)
        {
            side = PositionSide.Long;
            return true;
        }
        if (value?.Equals("SHORT", StringComparison.OrdinalIgnoreCase) == true)
        {
            side = PositionSide.Short;
            return true;
        }
        side = default;
        return false;
    }
}

