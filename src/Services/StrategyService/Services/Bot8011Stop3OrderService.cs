using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Execution;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Binance.Resilience;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Services;

public sealed class Bot8011Stop3OrderService(
    IOptions<Bot8011Options> options,
    IBinanceFuturesOrderClient orders,
    BinanceExchangeInfoService exchangeInfo,
    BinanceRetryService retry,
    ILogger<Bot8011Stop3OrderService> logger)
{
    private readonly Bot8011Options _options = options.Value;

    public async Task<CreatedStop3Order> CreateStop3WithFallbackAsync(
        BotPosition position,
        decimal quantity,
        string clientId,
        CancellationToken cancellationToken)
    {
        if (!position.EntryPrice.HasValue)
            throw new InvalidOperationException(
                $"Position {position.ShortId} has no entry price.");

        quantity = await exchangeInfo.RoundQuantityAsync(
            position.Symbol,
            quantity,
            cancellationToken);

        if (quantity <= 0)
            throw new InvalidOperationException(
                $"Invalid STOP3 quantity for {position.ShortId}.");

        var primary = position.Side == PositionSide.Long
            ? position.EntryPrice.Value + _options.Stop3EntryOffset
            : position.EntryPrice.Value - _options.Stop3EntryOffset;

        primary = await exchangeInfo.RoundPriceAsync(
            position.Symbol,
            primary,
            cancellationToken);

        try
        {
            return await CreateAsync(
                position,
                quantity,
                primary,
                clientId,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "BOT8011 STOP3 primary trigger rejected. Position={ShortId}, Trigger={Trigger}",
                position.ShortId,
                primary);
        }

        var fallback = await exchangeInfo.RoundPriceAsync(
            position.Symbol,
            position.EntryPrice.Value,
            cancellationToken);

        return await CreateAsync(
            position,
            quantity,
            fallback,
            clientId,
            cancellationToken);
    }

    public async Task<CreatedStop3Order> CreateTrailingStop3Async(
        BotPosition position,
        decimal triggerPrice,
        int sequence,
        CancellationToken cancellationToken)
    {
        triggerPrice = await exchangeInfo.RoundPriceAsync(
            position.Symbol,
            triggerPrice,
            cancellationToken);

        var quantity = await exchangeInfo.RoundQuantityAsync(
            position.Symbol,
            position.RemainingQuantity,
            cancellationToken);

        var clientId = BinanceClientOrderIdFactory.Create(
            _options.BotName,
            "STOP3",
            position.ShortId,
            sequence);

        return await CreateAsync(
            position,
            quantity,
            triggerPrice,
            clientId,
            cancellationToken);
    }

    private async Task<CreatedStop3Order> CreateAsync(
        BotPosition position,
        decimal quantity,
        decimal triggerPrice,
        string clientId,
        CancellationToken cancellationToken)
    {
        var order = await retry.ExecuteAsync(
            "BOT8011_CREATE_STOP3",
            ct => orders.PlaceStopMarketAlgoOrderAsync(
                position.Symbol,
                BinanceOrderSideMapper.ToCloseSide(position.Side),
                BinanceOrderSideMapper.ToPositionSide(position.Side),
                quantity,
                triggerPrice,
                clientId,
                ct),
            cancellationToken);

        return new CreatedStop3Order(
            order.AlgoOrderId,
            clientId,
            order.Status,
            triggerPrice);
    }
}

public sealed record CreatedStop3Order(
    string AlgoOrderId,
    string ClientAlgoId,
    string Status,
    decimal TriggerPrice);
