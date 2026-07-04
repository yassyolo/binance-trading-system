using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Binance.Orders;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Services;

public sealed class Bot8011Stop3OrderService
{
    private readonly Bot8011Options _options;
    private readonly IBinanceFuturesOrderClient _orders;
    private readonly BinanceExchangeInfoService _exchangeInfo;
    private readonly BinanceRetryService _retry;
    private readonly ILogger<Bot8011Stop3OrderService> _logger;

    public Bot8011Stop3OrderService(
        IOptions<Bot8011Options> options,
        IBinanceFuturesOrderClient orders,
        BinanceExchangeInfoService exchangeInfo,
        BinanceRetryService retry,
        ILogger<Bot8011Stop3OrderService> logger)
    {
        _options = options.Value;
        _orders = orders;
        _exchangeInfo = exchangeInfo;
        _retry = retry;
        _logger = logger;
    }

    public async Task<CreatedStop3Order> CreateStop3WithFallbackAsync(
        BotPosition position,
        decimal quantity,
        string clientId,
        CancellationToken cancellationToken)
    {
        if (!position.EntryPrice.HasValue)
            throw new InvalidOperationException($"Position {position.ShortId} has no entry price.");

        var firstPrice = position.Side == PositionSide.Long
            ? position.EntryPrice.Value + _options.Stop3EntryOffset
            : position.EntryPrice.Value - _options.Stop3EntryOffset;

        firstPrice = await _exchangeInfo.RoundPriceAsync(
            position.Symbol,
            firstPrice,
            cancellationToken);

        quantity = await _exchangeInfo.RoundQuantityAsync(
            position.Symbol,
            quantity,
            cancellationToken);

        if (quantity <= 0)
            throw new InvalidOperationException($"Invalid STOP3 quantity for {position.ShortId}.");

        try
        {
            var order = await _retry.ExecuteAsync(
                "CREATE_STOP3",
                ct => _orders.PlaceStopMarketAlgoOrderAsync(
                    position.Symbol,
                    ToCloseSide(position.Side),
                    ToPositionSide(position.Side),
                    quantity,
                    firstPrice,
                    clientId,
                    ct),
                cancellationToken);

            return new CreatedStop3Order(order.AlgoOrderId, order.Status, firstPrice);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "STOP3 first price rejected. Position={ShortId}, Price={Price}. Trying entry fallback.",
                position.ShortId,
                firstPrice);
        }

        var fallbackPrice = await _exchangeInfo.RoundPriceAsync(
            position.Symbol,
            position.EntryPrice.Value,
            cancellationToken);

        var fallbackOrder = await _retry.ExecuteAsync(
            "CREATE_STOP3_ENTRY_FALLBACK",
            ct => _orders.PlaceStopMarketAlgoOrderAsync(
                position.Symbol,
                ToCloseSide(position.Side),
                ToPositionSide(position.Side),
                quantity,
                fallbackPrice,
                clientId,
                ct),
            cancellationToken);

        return new CreatedStop3Order(
            fallbackOrder.AlgoOrderId,
            fallbackOrder.Status,
            fallbackPrice);
    }

    private static string ToCloseSide(PositionSide side)
        => side == PositionSide.Long ? "SELL" : "BUY";

    private static string ToPositionSide(PositionSide side)
        => side == PositionSide.Long ? "LONG" : "SHORT";
}

public sealed record CreatedStop3Order(
    string AlgoOrderId,
    string Status,
    decimal TriggerPrice);