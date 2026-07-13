using TradingSystem.Application.Engine;
using TradingSystem.Domain.Enums;

namespace TradingSystem.Application.Execution;

public sealed class CompositeTradeExecutor : ITradeExecutor
{
    private readonly IReadOnlyDictionary<string, IBotTradeExecutor> _executors;

    public CompositeTradeExecutor(IEnumerable<IBotTradeExecutor> executors)
    {
        _executors = executors.ToDictionary(
            x => x.BotName,
            x => x,
            StringComparer.OrdinalIgnoreCase);
    }

    public Task<TradeExecutionResult> OpenAsync(
        string botName,
        string symbol,
        PositionSide side,
        string? source,
        CancellationToken cancellationToken)
        => GetRequired(botName).OpenAsync(
            symbol,
            side,
            source,
            cancellationToken);

    public Task<TradeExecutionResult> CloseAsync(
        string botName,
        string shortId,
        string reason,
        CancellationToken cancellationToken)
        => GetRequired(botName).CloseAsync(
            shortId,
            reason,
            cancellationToken);

    private IBotTradeExecutor GetRequired(string botName)
    {
        if (!_executors.TryGetValue(botName, out var executor))
        {
            throw new InvalidOperationException(
                $"Trade executor is not registered for bot '{botName}'.");
        }

        return executor;
    }
}
