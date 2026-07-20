using TradingSystem.Domain.Enums;
namespace TradingSystem.Binance.Execution;
public static class BinanceClientOrderId
{
 public static string NewShortId() => Guid.NewGuid().ToString("N")[..8];
 public static string Create(string botName, string role, string shortId, int? sequence = null)
 { 
        var safeBot = new string(botName.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant(); 
        var suffix = sequence is null?"":$"_T{sequence}"; 
        var value = $"{safeBot}_{role.ToUpperInvariant()}_{shortId}{suffix}"; 
        return value[..Math.Min(32, value.Length)]; 
    }
 public static bool TryParse(string? value, out string botName, out string role, out string shortId)
 { 
        botName = role = shortId = string.Empty; 
        if(string.IsNullOrWhiteSpace(value))
            return false; 
        var p = value.Split('_', StringSplitOptions.RemoveEmptyEntries); 
        if(p.Length<3)
            return false; 
        botName = p[0].ToUpperInvariant();role = p[1].ToUpperInvariant();shortId = p[2];
        return true; 
    }
}
public static class BinanceOrderSide
{
 public static string Entry(PositionSide side) => side==PositionSide.Long?"BUY":"SELL";
 public static string Close(PositionSide side) => side==PositionSide.Long?"SELL":"BUY";
 public static string Position(PositionSide side) => side==PositionSide.Long?"LONG":"SHORT";
 public static bool TryParsePosition(string? value, out PositionSide side)
    {
        if(value?.Equals("LONG", StringComparison.OrdinalIgnoreCase)==true)
        {
            side = PositionSide.Long;
            return true;
        }
        if(value?.Equals("SHORT", StringComparison.OrdinalIgnoreCase)==true)
        {
            side = PositionSide.Short;
            return true;
        }
        side = default;
        return false;
    }
}
