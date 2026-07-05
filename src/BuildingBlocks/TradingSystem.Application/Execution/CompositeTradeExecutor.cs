using TradingSystem.Application.Engine;
using TradingSystem.Domain.Enums;

namespace StrategyService.Execution;

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

    public Task OpenAsync(
        string botName,
        string symbol,
        PositionSide side,
        string source,
        CancellationToken cancellationToken)
    {
        if (!_executors.TryGetValue(botName, out var executor))
            throw new InvalidOperationException($"Trade executor not found for bot: {botName}");

        return executor.OpenAsync(
            botName,
            symbol,
            side,
            source,
            cancellationToken);
    }
}