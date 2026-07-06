using TradingSystem.Application.Strategies;

namespace TradingSystem.Application.Engine;

public sealed class TradingEngine(
        IEnumerable<ITradingStrategy> strategies,
        IMarketPriceProvider marketPriceProvider,
        IActivePositionProvider activePositionProvider,
        ITradeExecutor tradeExecutor)
{
    private readonly IReadOnlyDictionary<string, ITradingStrategy> _strategies = strategies.ToDictionary(
        x => x.BotName,
        x => x,
        StringComparer.OrdinalIgnoreCase);
    public async Task<bool> ProcessSignalAsync(
        TradeSignal signal,
        CancellationToken cancellationToken)
    {
        if (!_strategies.TryGetValue(signal.BotName, out var strategy))
            return false;

        var markPrice = await marketPriceProvider.GetMarkPriceAsync(
            signal.Symbol,
            cancellationToken);

        var activePositions = await activePositionProvider.GetActivePositionsAsync(
            signal.BotName,
            signal.Symbol,
            cancellationToken);

        var context = new StrategyContext
        {
            Signal = signal,
            MarkPrice = markPrice,
            ActivePositions = activePositions
        };

        var decision = await strategy.DecideAsync(
            context,
            cancellationToken);

        if (decision.DecisionType is not StrategyDecisionType.Open and not StrategyDecisionType.OpenAfterClosing)
            return false;

        foreach (var shortId in decision.PositionsToClose)
        {
            await tradeExecutor.CloseAsync(
                signal.BotName,
                shortId,
                "OPPOSITE_SIGNAL",
                cancellationToken);
        }

        if (decision.PositionsToClose.Count > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        await tradeExecutor.OpenAsync(
            signal.BotName,
            signal.Symbol,
            signal.Side,
            signal.Source,
            cancellationToken);

        return true;
    }
}