namespace TradingSystem.Binance.Orders;

public interface IBinanceFuturesOrderClient
{
    Task<BinanceOrderResult> PlaceMarketOrderAsync(
        string symbol,
        string side,
        string positionSide,
        decimal quantity,
        string clientOrderId,
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

    Task<BinanceOrderResult> PlaceLimitOrderAsync(
    string symbol,
    string side,
    string positionSide,
    decimal quantity,
    decimal price,
    string clientOrderId,
    CancellationToken cancellationToken);
}