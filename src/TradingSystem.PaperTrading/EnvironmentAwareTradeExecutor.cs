using TradingSystem.Application.Execution;
using TradingSystem.BotRuntime.Configuration;
using TradingSystem.Domain.Enums;

namespace TradingSystem.PaperTrading;

public sealed class EnvironmentAwareTradeExecutor(
    TradeExecutorRegistry liveExecutor, 
    PaperTradeExecutor paperExecutor, 
    IBotRuntimeConfigurationProvider configurations) : ITradeExecutor
{
    public async Task<TradeExecutionResult> OpenAsync(string botName,  string symbol,  PositionSide side,  string? source,  CancellationToken cancellationToken)
    {
        var configuration  =  await configurations.GetAsync(botName,  cancellationToken);
        return IsPaper(configuration)
            ? await paperExecutor.OpenAsync(botName,  symbol,  side,  source,  cancellationToken)
            : await liveExecutor.OpenAsync(botName,  symbol,  side,  source,  cancellationToken);
    }

    public async Task<TradeExecutionResult> CloseAsync(string botName,  string shortId,  string reason,  CancellationToken cancellationToken)
    {
        var configuration  =  await configurations.GetAsync(botName,  cancellationToken);
        return IsPaper(configuration)
            ? await paperExecutor.CloseAsync(botName,  shortId,  reason,  cancellationToken)
            : await liveExecutor.CloseAsync(botName,  shortId,  reason,  cancellationToken);
    }

    private static bool IsPaper(BotRuntimeConfiguration? configuration)  => 
        configuration?.Environment.Equals("Paper",  StringComparison.OrdinalIgnoreCase) == true;
}
