using TradingSystem.Application.Engine;
using TradingSystem.Contracts.Signals;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Signals;

namespace StrategyService.Signals;

public sealed class SignalProcessor(
    TradingEngine engine,
    ILogger<SignalProcessor> logger)
{
    public async Task<bool> HandleAsync(
        TradingSignalMessage message,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseSide(message.Action, out var side))
        {
            logger.LogWarning(
                "Invalid signal action. SignalId={SignalId}, Action={Action}",
                message.SignalId,
                message.Action);

            return false;
        }

        if (string.IsNullOrWhiteSpace(message.BotName))
        {
            logger.LogWarning(
                "Trading signal has no target bot. SignalId={SignalId}, Source={Source}",
                message.SignalId,
                message.Source);

            return false;
        }

        var signal = new TradeSignal
        {
            SignalId = message.SignalId.Trim(),
            BotName = message.BotName.Trim(),
            Symbol = message.Symbol.Trim().ToUpperInvariant(),
            Side = side,
            Source = message.Source?.Trim(),
            GeneratedAtUtc = message.GeneratedAtUtc ?? DateTime.UtcNow
        };

        var result = await engine.ProcessSignalAsync(
            signal,
            cancellationToken);

        logger.LogInformation(
            "Trading signal processed. SignalId={SignalId}, Bot={Bot}, Opened={Opened}, Duplicate={Duplicate}, Reason={Reason}",
            signal.SignalId,
            signal.BotName,
            result.OpenedPosition,
            result.Duplicate,
            result.Reason);

        return result.Succeeded;
    }

    private static bool TryParseSide(
        string? action,
        out PositionSide side)
    {
        side = default;

        if (action?.Equals(
                "LONG",
                StringComparison.OrdinalIgnoreCase) == true)
        {
            side = PositionSide.Long;
            return true;
        }

        if (action?.Equals(
                "SHORT",
                StringComparison.OrdinalIgnoreCase) == true)
        {
            side = PositionSide.Short;
            return true;
        }

        return false;
    }
}
