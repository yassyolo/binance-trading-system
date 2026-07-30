using TradingSystem.Application.Execution;
using TradingSystem.BotRuntime.Configuration;
using TradingSystem.Domain.Enums;

namespace TradingSystem.PaperTrading;

public sealed class EnvironmentAwareTradeExecutor(
    TradeExecutorRegistry liveExecutor,
    PaperTradeExecutor paperExecutor,
    IBotRuntimeConfigurationProvider configurations) : ITradeExecutor
{
    public async Task<TradeExecutionResult> OpenAsync(string botName, string symbol, PositionSide side, string? source, CancellationToken cancellationToken)
    {
        var configuration = await configurations.GetAsync(botName, cancellationToken);
        if (configuration is null)
            return TradeExecutionResult.Failure($"Runtime configuration for '{botName}' was not found. Execution is blocked.");

        return IsPaper(configuration)
            ? await paperExecutor.OpenAsync(botName, symbol, side, source, cancellationToken)
            : IsLive(configuration)
                ? await liveExecutor.OpenAsync(botName, symbol, side, source, cancellationToken)
                : TradeExecutionResult.Failure($"Unsupported execution environment '{configuration.Environment}' for '{botName}'.");
    }

    public async Task<TradeExecutionResult> CloseAsync(string botName, string shortId, string reason, CancellationToken cancellationToken)
    {
        var configuration = await configurations.GetAsync(botName, cancellationToken);
        if (configuration is null)
            return TradeExecutionResult.Failure($"Runtime configuration for '{botName}' was not found. Execution is blocked.");

        return IsPaper(configuration)
            ? await paperExecutor.CloseAsync(botName, shortId, reason, cancellationToken)
            : IsLive(configuration)
                ? await liveExecutor.CloseAsync(botName, shortId, reason, cancellationToken)
                : TradeExecutionResult.Failure($"Unsupported execution environment '{configuration.Environment}' for '{botName}'.");
    }

    private static bool IsPaper(BotRuntimeConfiguration configuration) =>
        configuration.Environment.Equals("Paper", StringComparison.OrdinalIgnoreCase);

    private static bool IsLive(BotRuntimeConfiguration configuration) =>
        configuration.Environment.Equals("Demo", StringComparison.OrdinalIgnoreCase) ||
        configuration.Environment.Equals("Production", StringComparison.OrdinalIgnoreCase);
}
