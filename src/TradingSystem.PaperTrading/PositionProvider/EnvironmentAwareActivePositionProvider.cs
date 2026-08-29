using TradingSystem.Application.Positions;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Positions.Models;
using TradingSystem.BotRuntime.Configuration.Contracts;

namespace TradingSystem.PaperTrading.Position;

public sealed class EnvironmentAwareActivePositionProvider(
    ActivePositionProviderRegistry livePositionProvider,
    PaperActivePositionProvider paperPositionProvider,
    IBotRuntimeConfigurationProvider configProvider) 
    : IActivePositionProvider
{
    public async Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(string botName, string symbol, CancellationToken ct)
    {
        var config = await configProvider.GetAsync(botName, ct)
            ?? throw new InvalidOperationException($"Runtime config for '{botName}' was not found. Position lookup is blocked.");

        if (config.Environment.Equals("Paper", StringComparison.OrdinalIgnoreCase))
            return await paperPositionProvider.GetAsync(botName, symbol, ct);

        if (config.Environment.Equals("Demo", StringComparison.OrdinalIgnoreCase) ||
            config.Environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
            return await livePositionProvider.GetActivePositionsAsync(botName, symbol, ct);

        throw new InvalidOperationException($"Unsupported execution environment '{config.Environment}' for '{botName}'.");
    }
}
