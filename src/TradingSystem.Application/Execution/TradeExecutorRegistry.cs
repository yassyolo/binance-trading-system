using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Execution.Models;
using TradingSystem.Domain.Enums;

namespace TradingSystem.Application.Execution;

public sealed class TradeExecutorRegistry : ITradeExecutor
{
    private readonly IReadOnlyDictionary<string,  IBotTradeExecutor> _executors;

    public TradeExecutorRegistry(IEnumerable<IBotTradeExecutor> executors)
    {
        _executors  =  BuildUniqueMap(executors);
    }

    public Task<TradeExecutionResult> OpenAsync(string botName,  string symbol,  PositionSide side,  string? source,  CancellationToken ct)
         =>  GetRequired(botName).OpenAsync(symbol,  side,  source,  ct);

    public Task<TradeExecutionResult> CloseAsync(string botName,  string shortId,  string reason,  CancellationToken ct)
         =>  GetRequired(botName).CloseAsync(shortId,  reason,  ct);

    private IBotTradeExecutor GetRequired(string botName)
         =>  _executors.TryGetValue(botName,  out var executor)
            ? executor
            : throw new InvalidOperationException($"Trade executor is not registered for bot '{botName}'.");

    private static IReadOnlyDictionary<string,  IBotTradeExecutor> BuildUniqueMap(IEnumerable<IBotTradeExecutor> executors)
    {
        var map  =  new Dictionary<string,  IBotTradeExecutor>(StringComparer.OrdinalIgnoreCase);
        foreach (var executor in executors)
        {
            if (!map.TryAdd(executor.BotName,  executor))
                throw new InvalidOperationException($"Multiple trade executors are registered for bot '{executor.BotName}'.");
        }
        return map;
    }
}
