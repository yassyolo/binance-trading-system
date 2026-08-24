using TradingSystem.Contracts.Signals;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Signals;

namespace TradingSystem.Redis.Signals;

internal static class TradingSignalMessageMapper
{
    public static TradeSignal Map(TradingSignalMessage message,  DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.SignalId))
            throw new InvalidOperationException("SignalId is required.");

        if (string.IsNullOrWhiteSpace(message.BotName))
            throw new InvalidOperationException("BotName is required.");

        if (string.IsNullOrWhiteSpace(message.Symbol))
            throw new InvalidOperationException("Symbol is required.");

        if (string.IsNullOrWhiteSpace(message.Action))
            throw new InvalidOperationException("Action is required.");

        if (string.IsNullOrWhiteSpace(message.Source))
            throw new InvalidOperationException("Source is required.");

        if (!Enum.TryParse<PositionSide>(message.Action, true, out var side))
            throw new InvalidOperationException($"Unsupported signal action '{message.Action}'. Expected LONG or SHORT.");

        var generatedAtUtc = message.GeneratedAtUtc?.ToUniversalTime() ?? DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);

        return new TradeSignal
        {
            SignalId = message.SignalId.Trim(),
            BotName = message.BotName.Trim(),
            Symbol = message.Symbol.Trim().ToUpperInvariant(),
            Side = side,
            Source = message.Source.Trim(),
            GeneratedAtUtc = generatedAtUtc
        };
    }
}
