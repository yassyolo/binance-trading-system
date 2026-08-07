using Microsoft.Extensions.Options;
using TradingSystem.Binance.Exchange;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Binance.Resilience;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;
using StrategyService.Bots.Bot8015.Configuration;
using TradingSystem.Binance.Execution.Models;
using StrategyService.Bots.Bot8015.Models;

namespace StrategyService.Bots.Bot8015;

public sealed class Bot8015Stop3OrderService(
    IOptions<Bot8015Options> options,
    IBinanceFuturesOrderClient orders,
    BinanceExchangeInfoService exchange,
    BinanceRetryService retry)
{
    private readonly Bot8015Options _options = options.Value;

    public Task<CreatedStop3Order> CreateInitialAsync(BotPosition position, decimal quantity, CancellationToken ct) =>
        CreateWithFallbackAsync(position, quantity, BinanceClientOrderId.Create(_options.BotName, "S3", position.ShortId), ct);

    public async Task<CreatedStop3Order> CreateTrailingAsync(BotPosition position, decimal trigger, int sequence, CancellationToken ct) =>
        await CreateAsync(position,
            await exchange.RoundQuantityAsync(position.Symbol, position.RemainingQuantity, ct),
            await exchange.RoundPriceAsync(position.Symbol, trigger, ct),
            BinanceClientOrderId.Create(_options.BotName, "S3", position.ShortId, sequence), ct);

    private async Task<CreatedStop3Order> CreateWithFallbackAsync(BotPosition position, decimal quantity, string clientId, CancellationToken ct)
    {
        if (position.EntryPrice is null) 
            throw new InvalidOperationException("EntryPrice is required.");
        
        quantity = await exchange.RoundQuantityAsync(position.Symbol, quantity, ct);
        
        var primary = await exchange.RoundPriceAsync(position.Symbol, position.Side == PositionSide.Long ? position.EntryPrice.Value + _options.Stop3EntryOffset : position.EntryPrice.Value - _options.Stop3EntryOffset, ct);
        
        try 
        { 
            return await CreateAsync(position, quantity, primary, clientId, ct); 
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) 
        { 
            throw; 
        }      
        catch
        {
            var fallback = await exchange.RoundPriceAsync(position.Symbol, position.EntryPrice.Value, ct);
            
            return await CreateAsync(position, quantity, fallback, clientId, ct);
        }
    }

    private async Task<CreatedStop3Order> CreateAsync(BotPosition position, decimal quantity, decimal price, string clientId, CancellationToken ct)
    {
        if (quantity <= 0) 
            throw new InvalidOperationException("STOP3 quantity is invalid.");
        
        if (price <= 0) 
            throw new InvalidOperationException("STOP3 trigger price is invalid.");
       
        var result = await retry.ExecuteAsync("BOT8015_STOP3", c => 
                orders.PlaceStopMarketAlgoOrderAsync(position.Symbol, BinanceOrderSide.Close(position.Side), BinanceOrderSide.Position(position.Side), quantity, price, clientId, c), ct);
        
        return new(result.AlgoOrderId, clientId, result.Status, price);
    }
}
