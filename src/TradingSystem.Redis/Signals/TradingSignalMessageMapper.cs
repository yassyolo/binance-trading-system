using TradingSystem.Contracts.Signals;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Signals;

namespace TradingSystem.Redis.Signals;

internal static class TradingSignalMessageMapper
{
    public static TradeSignal Map(TradingSignalMessage message,  DateTime nowUtc)
    {
        if (!Enum.TryParse<PositionSide>(message.Action,  true,  out var side))
            throw new InvalidOperationException($"Unsupported signal action '{message.Action}'. Expected LONG or SHORT.");
       
        return new TradeSignal
        {
            SignalId = message.SignalId, 
            BotName = message.BotName.Trim(), 
            Symbol = message.Symbol.Trim().ToUpperInvariant(), 
            Side = side, 
            Source = message.Source!, 
            GeneratedAtUtc = message.GeneratedAtUtc?.ToUniversalTime()??nowUtc
        };
    }
}
