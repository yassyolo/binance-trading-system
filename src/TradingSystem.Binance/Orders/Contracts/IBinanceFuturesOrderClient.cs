using TradingSystem.Binance.Orders.Models;
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
        CancellationToken ct);

    Task<BinanceOrderResult> PlaceLimitOrderAsync(
        string symbol,
        string side,
        string positionSide,
        decimal quantity,
        decimal price,
        string clientOrderId,
        CancellationToken ct);

    Task<BinanceAlgoOrderResult> PlaceTakeProfitMarketAlgoOrderAsync(
        string symbol,
        string side,
        string positionSide,
        decimal quantity,
        decimal stopPrice,
        string clientOrderId,
        CancellationToken ct);

    Task<BinanceAlgoOrderResult> PlaceStopMarketAlgoOrderAsync(
        string symbol,
        string side,
        string positionSide,
        decimal quantity,
        decimal stopPrice,
        string clientOrderId,
        CancellationToken ct);

    Task<BinanceOrderResult> GetOrderAsync(
        string symbol,
        string orderId,
        CancellationToken ct);

    Task<BinanceOrderResult> GetOrderByClientOrderIdAsync(
        string symbol,
        string clientOrderId,
        CancellationToken ct);

    Task CancelOrderAsync(
        string symbol,
        string orderId,
        CancellationToken ct);

    Task CancelAlgoOrderAsync(
        string symbol,
        string algoOrderId,
        CancellationToken ct);

    Task<IReadOnlyCollection<BinanceOpenOrder>> GetOpenOrdersAsync(
        string symbol,
        CancellationToken ct);

    Task<IReadOnlyCollection<BinanceOpenAlgoOrder>> GetOpenAlgoOrdersAsync(
        string symbol,
        CancellationToken ct);

    Task<BinanceSymbolFilters> GetSymbolFiltersAsync(
        string symbol,
        CancellationToken ct);

    Task SetHedgeModeAsync(CancellationToken ct);

    Task SetLeverageAsync(
        string symbol,
        int leverage,
        CancellationToken ct);

    Task<IReadOnlyCollection<BinancePositionRisk>> GetPositionRiskAsync(
        string symbol,
        CancellationToken ct);

    Task<decimal> GetMarkPriceAsync(
        string symbol,
        CancellationToken ct);

    Task<IReadOnlyCollection<BinanceTradeFill>> GetTradeFillsForOrderAsync(
    string symbol,
    string orderId,
    CancellationToken ct);
}
