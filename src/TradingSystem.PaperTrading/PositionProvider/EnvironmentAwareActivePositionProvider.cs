using TradingSystem.Application.Positions;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Positions.Models;
using TradingSystem.BotRuntime.Configuration.Contracts;

namespace TradingSystem.PaperTrading.Position;

public sealed class EnvironmentAwareActivePositionProvider(
    ActivePositionProviderRegistry liveProvider,
    PaperActivePositionProvider paperProvider,
    IBotRuntimeConfigurationProvider configurations) 
    : IActivePositionProvider
{
    public async Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(string botName, string symbol, CancellationToken ct)
    {
        var configuration = await configurations.GetAsync(botName, ct)
            ?? throw new InvalidOperationException($"Runtime configuration for '{botName}' was not found. Position lookup is blocked.");

        if (configuration.Environment.Equals("Paper", StringComparison.OrdinalIgnoreCase))
            return await paperProvider.GetAsync(botName, symbol, ct);

        if (configuration.Environment.Equals("Demo", StringComparison.OrdinalIgnoreCase) ||
            configuration.Environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
        {
            return await liveProvider.GetActivePositionsAsync(botName, symbol, ct);
        }

        throw new InvalidOperationException($"Unsupported execution environment '{configuration.Environment}' for '{botName}'.");
    }
}
