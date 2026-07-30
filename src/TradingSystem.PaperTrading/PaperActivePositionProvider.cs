using TradingSystem.Application.Positions;
using TradingSystem.BotRuntime.Configuration;

namespace TradingSystem.PaperTrading;

public sealed class PaperActivePositionProvider(IPaperTradingStore store)
{
    public async Task<IReadOnlyCollection<ActivePositionView>> GetAsync(string botName, string symbol, CancellationToken cancellationToken)
    {
        var positions = await store.QueryAsync(botName, symbol, PaperPositionStatus.Open, 0, 500, cancellationToken);
        return positions.Select(x => new ActivePositionView
        {
            ShortId = x.ShortId,
            BotName = x.BotName,
            Symbol = x.Symbol,
            Side = x.Side,
            EntryPrice = x.EntryPrice,
            Quantity = x.Quantity,
            RemainingQuantity = x.Quantity,
            TpPrice = x.TakeProfitPrice,
            CreatedAtUtc = x.OpenedAtUtc
        }).ToArray();
    }
}

public sealed class EnvironmentAwareActivePositionProvider(
    ActivePositionProviderRegistry liveProvider,
    PaperActivePositionProvider paperProvider,
    IBotRuntimeConfigurationProvider configurations) : IActivePositionProvider
{
    public async Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(string botName, string symbol, CancellationToken cancellationToken)
    {
        var configuration = await configurations.GetAsync(botName, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Runtime configuration for '{botName}' was not found. Position lookup is blocked.");

        if (configuration.Environment.Equals("Paper", StringComparison.OrdinalIgnoreCase))
            return await paperProvider.GetAsync(botName, symbol, cancellationToken);

        if (configuration.Environment.Equals("Demo", StringComparison.OrdinalIgnoreCase) ||
            configuration.Environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
        {
            return await liveProvider.GetActivePositionsAsync(botName, symbol, cancellationToken);
        }

        throw new InvalidOperationException(
            $"Unsupported execution environment '{configuration.Environment}' for '{botName}'.");
    }
}
