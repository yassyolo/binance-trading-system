using Microsoft.Extensions.Options;
using TradingSystem.Binance.Execution;
using TradingSystem.Binance.Exchange;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Binance.Resilience;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Bots.Bot8015;

public sealed record CreatedStop3Order(string AlgoOrderId, string ClientAlgoId, string? Status, decimal TriggerPrice);

public sealed class Bot8015Stop3OrderService(
    IOptions<Bot8015Options> options,
    IBinanceFuturesOrderClient orders,
    BinanceExchangeInfoService exchange,
    BinanceRetryService retry)
{
    private readonly Bot8015Options _options = options.Value;

    public Task<CreatedStop3Order> CreateInitialAsync(BotPosition position, decimal quantity, CancellationToken cancellationToken) =>
        CreateWithFallbackAsync(position, quantity, BinanceClientOrderId.Create(_options.BotName, "S3", position.ShortId), cancellationToken);

    public async Task<CreatedStop3Order> CreateTrailingAsync(BotPosition position, decimal trigger, int sequence, CancellationToken cancellationToken) =>
        await CreateAsync(position,
            await exchange.RoundQuantityAsync(position.Symbol, position.RemainingQuantity, cancellationToken),
            await exchange.RoundPriceAsync(position.Symbol, trigger, cancellationToken),
            BinanceClientOrderId.Create(_options.BotName, "S3", position.ShortId, sequence), cancellationToken);

    private async Task<CreatedStop3Order> CreateWithFallbackAsync(BotPosition position, decimal quantity, string clientId, CancellationToken cancellationToken)
    {
        if (position.EntryPrice is null) throw new InvalidOperationException("EntryPrice is required.");
        quantity = await exchange.RoundQuantityAsync(position.Symbol, quantity, cancellationToken);
        var primary = await exchange.RoundPriceAsync(position.Symbol,
            position.Side == PositionSide.Long ? position.EntryPrice.Value + _options.Stop3EntryOffset : position.EntryPrice.Value - _options.Stop3EntryOffset,
            cancellationToken);
        try { return await CreateAsync(position, quantity, primary, clientId, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch
        {
            var fallback = await exchange.RoundPriceAsync(position.Symbol, position.EntryPrice.Value, cancellationToken);
            return await CreateAsync(position, quantity, fallback, clientId, cancellationToken);
        }
    }

    private async Task<CreatedStop3Order> CreateAsync(BotPosition position, decimal quantity, decimal price, string clientId, CancellationToken cancellationToken)
    {
        if (quantity <= 0) throw new InvalidOperationException("STOP3 quantity is invalid.");
        if (price <= 0) throw new InvalidOperationException("STOP3 trigger price is invalid.");
        var result = await retry.ExecuteAsync("BOT8015_STOP3", c => orders.PlaceStopMarketAlgoOrderAsync(
            position.Symbol, BinanceOrderSide.Close(position.Side), BinanceOrderSide.Position(position.Side), quantity, price, clientId, c), cancellationToken);
        return new(result.AlgoOrderId, clientId, result.Status, price);
    }
}
