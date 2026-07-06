using TradingSystem.Application.Engine;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Signals;

namespace StrategyService.Signals;

public sealed class SignalProcessor(
        TradingEngine engine,
        ILogger<SignalProcessor> logger) : ITradingSignalHandler
{
    public async Task<bool> HandleAsync(
        TradingSignal signal,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseSide(signal.Action, out var side))
        {
            logger.LogWarning(
                "Invalid signal action. Action={Action}",
                signal.Action);

            return false;
        }

        var botName = string.IsNullOrWhiteSpace(signal.Source)
            ? "BOT8012"
            : signal.Source.Trim();

        var tradeSignal = new TradeSignal
        {
            BotName = botName,
            Symbol = signal.Symbol,
            Side = side,
            Source = signal.Source
        };

        return await engine.ProcessSignalAsync(
            tradeSignal,
            cancellationToken);
    }

    private static bool TryParseSide(string action, out PositionSide side)
    {
        side = default;

        if (action.Equals("long", StringComparison.OrdinalIgnoreCase))
        {
            side = PositionSide.Long;
            return true;
        }

        if (action.Equals("short", StringComparison.OrdinalIgnoreCase))
        {
            side = PositionSide.Short;
            return true;
        }

        return false;
    }
}