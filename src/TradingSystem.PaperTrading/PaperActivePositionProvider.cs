using TradingSystem.Application.Positions;
using TradingSystem.BotRuntime.Configuration;

namespace TradingSystem.PaperTrading;

public sealed class PaperActivePositionProvider(IPaperTradingStore store)
{
    public async Task<IReadOnlyCollection<ActivePositionView>> GetAsync(string botName,  string symbol,  CancellationToken cancellationToken)
    {
        var positions  =  await store.QueryAsync(botName,  symbol,  PaperPositionStatus.Open,  0,  500,  cancellationToken);
        return positions.Select(x  =>  new ActivePositionView
        {
            ShortId  =  x.ShortId, 
            BotName  =  x.BotName, 
            Symbol  =  x.Symbol, 
            Side  =  x.Side, 
            EntryPrice  =  x.EntryPrice, 
            Quantity  =  x.Quantity, 
            RemainingQuantity  =  x.Quantity, 
            TpPrice  =  x.TakeProfitPrice, 
            CreatedAtUtc  =  x.OpenedAtUtc
        }).ToArray();
    }
}

public sealed class EnvironmentAwareActivePositionProvider(
    ActivePositionProviderRegistry liveProvider, 
    PaperActivePositionProvider paperProvider, 
    IBotRuntimeConfigurationProvider configurations) : IActivePositionProvider
{
    public async Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(string botName,  string symbol,  CancellationToken cancellationToken)
    {
        var configuration  =  await configurations.GetAsync(botName,  cancellationToken);
        return configuration?.Environment.Equals("Paper",  StringComparison.OrdinalIgnoreCase) == true
            ? await paperProvider.GetAsync(botName,  symbol,  cancellationToken)
            : await liveProvider.GetActivePositionsAsync(botName,  symbol,  cancellationToken);
    }
}
