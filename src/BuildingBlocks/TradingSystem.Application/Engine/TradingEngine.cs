using TradingSystem.Application.Strategies;

namespace TradingSystem.Application.Engine;

public sealed class TradingEngine
{
    private readonly IReadOnlyDictionary<string, ITradingStrategy> _strategies;
    private readonly IMarketPriceProvider _marketPriceProvider;
    private readonly IActivePositionProvider _activePositionProvider;
    private readonly ITradeExecutor _tradeExecutor;

    public TradingEngine(
        IEnumerable<ITradingStrategy> strategies,
        IMarketPriceProvider marketPriceProvider,
        IActivePositionProvider activePositionProvider,
        ITradeExecutor tradeExecutor)
    {
        _strategies = strategies.ToDictionary(
            x => x.BotName,
            x => x,
            StringComparer.OrdinalIgnoreCase);

        _marketPriceProvider = marketPriceProvider;
        _activePositionProvider = activePositionProvider;
        _tradeExecutor = tradeExecutor;
    }

    public async Task<bool> ProcessSignalAsync(
        TradeSignal signal,
        CancellationToken cancellationToken)
    {
        if (!_strategies.TryGetValue(signal.BotName, out var strategy))
            return false;

        var markPrice = await _marketPriceProvider.GetMarkPriceAsync(
            signal.Symbol,
            cancellationToken);

        var activePositions = await _activePositionProvider.GetActivePositionsAsync(
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

        if (decision.DecisionType != StrategyDecisionType.Open)
            return false;

        await _tradeExecutor.OpenAsync(
            signal.BotName,
            signal.Symbol,
            signal.Side,
            signal.Source,
            cancellationToken);

        return true;
    }
}