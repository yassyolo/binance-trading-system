using TradingSystem.Binance.Positions;

namespace TradingSystem.Binance.Orders.Contracts;

public interface IBinanceFuturesOrderClient
{
    Task<BinanceOrderResult> PlaceMarketOrderAsync(
        string symbol,
        string side,
        string positionSide,
        decimal quantity,
        string clientOrderId,
        CancellationToken cancellationToken);

    Task<BinanceOrderResult> PlaceLimitOrderAsync(
        string symbol,
        string side,
        string positionSide,
        decimal quantity,
        decimal price,
        string clientOrderId,
        CancellationToken cancellationToken);

    Task<BinanceOrderResult> GetOrderAsync(
        string symbol,
        string orderId,
        CancellationToken cancellationToken);

    Task<BinanceSymbolFilters> GetSymbolFiltersAsync(
        string symbol,
        CancellationToken cancellationToken);

    Task<BinanceAlgoOrderResult> PlaceTakeProfitMarketAlgoOrderAsync(
        string symbol,
        string side,
        string positionSide,
        decimal quantity,
        decimal stopPrice,
        string clientOrderId,
        CancellationToken cancellationToken);

    Task<BinanceAlgoOrderResult> PlaceStopMarketAlgoOrderAsync(
        string symbol,
        string side,
        string positionSide,
        decimal quantity,
        decimal stopPrice,
        string clientOrderId,
        CancellationToken cancellationToken);

    Task CancelOrderAsync(
        string symbol,
        string orderId,
        CancellationToken cancellationToken);

    Task CancelAlgoOrderAsync(
        string symbol,
        string algoOrderId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<BinanceOpenOrder>> GetOpenOrdersAsync(
        string symbol,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<BinanceOpenAlgoOrder>> GetOpenAlgoOrdersAsync(
        string symbol,
        CancellationToken cancellationToken);

    Task SetHedgeModeAsync(CancellationToken cancellationToken);

    Task SetLeverageAsync(
        string symbol,
        int leverage,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<BinancePositionRisk>> GetPositionRiskAsync(
        string symbol,
        CancellationToken cancellationToken);
}