using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Services;

public sealed class Bot8015Stop3OrderService(
    IOptions<Bot8015Options> options,
    IBinanceFuturesOrderClient orders,
    ILogger<Bot8015Stop3OrderService> logger)
{
    private readonly Bot8015Options _options = options.Value;

    public async Task CreateAfterTpAsync(
        BotPosition position,
        CancellationToken cancellationToken)
    {
        if (position.RemainingQuantity <= 0)
            throw new InvalidOperationException("STOP3 requires positive remaining quantity.");

        var filters = await orders.GetSymbolFiltersAsync(
            position.Symbol,
            cancellationToken);

        var primaryRaw = position.Side == PositionSide.Long
            ? position.EntryPrice + _options.Stop3EntryOffset
            : position.EntryPrice - _options.Stop3EntryOffset;

        var primary = QuantizeDown(primaryRaw.Value, filters.TickSize);

        try
        {
            await CreateAsync(
                position,
                primary,
                cancellationToken);

            return;
        }
        catch (Exception ex) when (primary != position.EntryPrice)
        {
            logger.LogWarning(
                ex,
                "BOT8015 primary STOP3 creation failed. Falling back to entry price. ShortId={ShortId}, Trigger={Trigger}",
                position.ShortId,
                primary);
        }

        var fallback = QuantizeDown(
            position.EntryPrice.Value,
            filters.TickSize);

        await CreateAsync(
            position,
            fallback,
            cancellationToken);
    }

    public async Task ReplaceAsync(
        BotPosition position,
        decimal newTrigger,
        CancellationToken cancellationToken)
    {
        if (position.RemainingQuantity <= 0)
            throw new InvalidOperationException("STOP3 replacement requires positive remaining quantity.");

        var previousOrderId = position.Stop3OrderId;

        if (!string.IsNullOrWhiteSpace(previousOrderId))
        {
            await orders.CancelAlgoOrderAsync(
                position.Symbol,
                previousOrderId,
                cancellationToken);
        }

        var nextTrailCount = position.TrailCount + 1;
        var clientId = CreateClientId(
            position.ShortId,
            nextTrailCount);

        var created = await orders.PlaceStopMarketAlgoOrderAsync(
            position.Symbol,
            ToCloseSide(position.Side),
            ToPositionSide(position.Side),
            position.RemainingQuantity,
            newTrigger,
            clientId,
            cancellationToken);

        position.Stop3Previous = position.Stop3Current;
        position.Stop3Current = newTrigger;
        position.Stop3ClientId = clientId;
        position.Stop3OrderId = created.AlgoOrderId;
        position.Stop3Status = created.Status;
        position.Stop3Created = true;
        position.Stop3Pending = false;
        position.ProtectiveActive = true;
        position.TrailCount = nextTrailCount;
    }

    private async Task CreateAsync(
        BotPosition position,
        decimal trigger,
        CancellationToken cancellationToken)
    {
        var clientId = CreateClientId(
            position.ShortId,
            trailCount: 0);

        var created = await orders.PlaceStopMarketAlgoOrderAsync(
            position.Symbol,
            ToCloseSide(position.Side),
            ToPositionSide(position.Side),
            position.RemainingQuantity,
            trigger,
            clientId,
            cancellationToken);

        position.Stop3Initial = trigger;
        position.Stop3Current = trigger;
        position.Stop3Previous = trigger;
        position.Stop3ClientId = clientId;
        position.Stop3OrderId = created.AlgoOrderId;
        position.Stop3Status = created.Status;
        position.Stop3Created = true;
        position.Stop3Pending = false;
        position.ProtectiveActive = true;
    }

    private string CreateClientId(
        string shortId,
        int trailCount)
    {
        var shortBot = _options.BotName.Length > 8
            ? _options.BotName[..8]
            : _options.BotName;

        var suffix = trailCount > 0
            ? $"_T{trailCount}"
            : string.Empty;

        var clientId = $"{shortBot}_STOP3_{shortId}{suffix}";

        return clientId.Length <= 32
            ? clientId
            : clientId[..32];
    }

    private static decimal QuantizeDown(
        decimal value,
        decimal tickSize)
    {
        if (tickSize <= 0)
            return value;

        return Math.Floor(value / tickSize) * tickSize;
    }

    private static string ToCloseSide(PositionSide side)
        => side == PositionSide.Long ? "SELL" : "BUY";

    private static string ToPositionSide(PositionSide side)
        => side == PositionSide.Long ? "LONG" : "SHORT";
}
