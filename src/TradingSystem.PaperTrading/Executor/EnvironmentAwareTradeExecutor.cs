using TradingSystem.Application.Execution;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Execution.Models;
using TradingSystem.BotRuntime.Configuration.Contracts;
using TradingSystem.BotRuntime.Configuration.Models;
using TradingSystem.Domain.Enums;

namespace TradingSystem.PaperTrading.Executor;

public sealed class EnvironmentAwareTradeExecutor(
    TradeExecutorRegistry liveExecutor,
    PaperTradeExecutor paperExecutor,
    IBotRuntimeConfigurationProvider configurations) 
    : ITradeExecutor
{
    public async Task<TradeExecutionResult> OpenAsync(string botName, string symbol, PositionSide side, string? source, CancellationToken ct)
    {
        var config = await configurations.GetAsync(botName, ct);
        if (config is null)
            return TradeExecutionResult.Failure($"Runtime config for '{botName}' was not found. Execution is blocked.");

        return IsPaper(config)
            ? await paperExecutor.OpenAsync(botName, symbol, side, source, ct)
            : IsLive(config)
                ? await liveExecutor.OpenAsync(botName, symbol, side, source, ct)
                : TradeExecutionResult.Failure($"Unsupported execution environment '{config.Environment}' for '{botName}'.");
    }

    public async Task<TradeExecutionResult> CloseAsync(string botName, string shortId, string reason, CancellationToken ct)
    {
        var config = await configurations.GetAsync(botName, ct);
        if (config is null)
            return TradeExecutionResult.Failure($"Runtime config for '{botName}' was not found. Execution is blocked.");

        return IsPaper(config)
            ? await paperExecutor.CloseAsync(botName, shortId, reason, ct)
            : IsLive(config)
                ? await liveExecutor.CloseAsync(botName, shortId, reason, ct)
                : TradeExecutionResult.Failure($"Unsupported execution environment '{config.Environment}' for '{botName}'.");
    }

    private static bool IsPaper(BotRuntimeConfiguration config) =>
        config.Environment.Equals("Paper", StringComparison.OrdinalIgnoreCase);

    private static bool IsLive(BotRuntimeConfiguration config) =>
        config.Environment.Equals("Demo", StringComparison.OrdinalIgnoreCase) ||
        config.Environment.Equals("Production", StringComparison.OrdinalIgnoreCase);
}
