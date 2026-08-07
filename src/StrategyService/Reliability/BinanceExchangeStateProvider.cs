using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Reconciliation.Contracts;
using TradingSystem.Reconciliation.Models;

namespace StrategyService.Reliability;

public sealed class BinanceExchangeStateProvider(
    IBinanceFuturesOrderClient client) 
    : IExchangeStateProvider
{
    public async Task<ExchangeStateSnapshot> GetAsync(string symbol, CancellationToken ct)
    {
        var positionsTask = client.GetPositionRiskAsync(symbol, ct);
        var normalTask = client.GetOpenOrdersAsync(symbol, ct);
        var algoTask = client.GetOpenAlgoOrdersAsync(symbol, ct);

        await Task.WhenAll(positionsTask, normalTask, algoTask);

        var positions = (await positionsTask)
            .Where(x => x.PositionAmount != 0)
            .Select(x => new ExchangePositionSnapshot(
                x.Symbol,
                x.PositionSide,
                Math.Abs(x.PositionAmount),
                x.EntryPrice))
            .ToArray();

        var normal = (await normalTask)
            .Select(x => new ExchangeOrderSnapshot(
                x.Symbol,
                x.ClientOrderId,
                x.Type,
                x.Quantity,
                null));

        var algo = (await algoTask)
            .Select(x => new ExchangeOrderSnapshot(
                x.Symbol,
                x.ClientAlgoId,
                x.OrderType,
                x.Quantity,
                x.TriggerPrice));

        return new ExchangeStateSnapshot(positions, normal.Concat(algo).ToArray());
    }
}


