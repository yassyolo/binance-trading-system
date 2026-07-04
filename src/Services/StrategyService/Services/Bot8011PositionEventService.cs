using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Orders;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Services;

public sealed class Bot8011PositionEventService
{
    private readonly Bot8011Options _options;
    private readonly IPositionStore _positionStore;
    private readonly IBinanceFuturesOrderClient _binanceOrders;
    private readonly ILogger<Bot8011PositionEventService> _logger;

    public Bot8011PositionEventService(
        IOptions<Bot8011Options> options,
        IPositionStore positionStore,
        IBinanceFuturesOrderClient binanceOrders,
        ILogger<Bot8011PositionEventService> logger)
    {
        _options = options.Value;
        _positionStore = positionStore;
        _binanceOrders = binanceOrders;
        _logger = logger;
    }

    public async Task HandleTpFilledAsync(
        string shortId,
        decimal executedQuantity,
        CancellationToken cancellationToken)
    {
        var position = await _positionStore.GetAsync(
            _options.BotName,
            shortId,
            cancellationToken);

        if (position is null || position.Closed)
            return;

        if (position.TpExecuted || position.Stop3Created || position.Stop3Pending)
            return;

        var remainingQuantity = position.Quantity - executedQuantity;

        if (remainingQuantity <= 0)
        {
            position.TpExecuted = true;
            position.TpFilledAtUtc = DateTime.UtcNow;
            position.TpStatus = "FILLED";
            position.RemainingQuantity = 0;

            await MarkClosedAsync(position, "TP_FULL_EXIT", cancellationToken);
            return;
        }

        position.TpExecuted = true;
        position.TpFilledAtUtc = DateTime.UtcNow;
        position.TpStatus = "FILLED";
        position.RemainingQuantity = remainingQuantity;
        position.Stop3Pending = true;
        position.ProtectiveActive = true;
        position.UpdatedAtUtc = DateTime.UtcNow;

        await _positionStore.SaveAsync(position, cancellationToken);

        try
        {
            if (!string.IsNullOrWhiteSpace(position.SlOrderId))
            {
                await _binanceOrders.CancelAlgoOrderAsync(
                    position.Symbol,
                    position.SlOrderId,
                    cancellationToken);

                position.SlStatus = "CANCELED";
            }

            var stop3ClientId = CreateClientId(_options.BotName, "S3", position.ShortId);
            var stop3Price = CalculateInitialStop3Price(position);

            var stop3 = await _binanceOrders.PlaceStopMarketAlgoOrderAsync(
                position.Symbol,
                ToCloseSide(position.Side),
                ToPositionSide(position.Side),
                remainingQuantity,
                stop3Price,
                stop3ClientId,
                cancellationToken);

            position.Stop3ClientId = stop3ClientId;
            position.Stop3OrderId = stop3.AlgoOrderId;
            position.Stop3Initial = stop3Price;
            position.Stop3Current = stop3Price;
            position.Stop3Previous = stop3Price;
            position.Stop3Status = stop3.Status;
            position.Stop3Created = true;
            position.Stop3Pending = false;
            position.ProtectiveActive = true;
            position.UpdatedAtUtc = DateTime.UtcNow;

            await _positionStore.SaveAsync(position, cancellationToken);

            _logger.LogInformation(
                "BOT8011 TP filled. STOP3 created. Position={ShortId}, RemainingQty={RemainingQuantity}, Stop3={Stop3Price}",
                shortId,
                remainingQuantity,
                stop3Price);
        }
        catch (Exception ex)
        {
            position.Stop3Pending = true;
            position.Stop3Created = false;
            position.UpdatedAtUtc = DateTime.UtcNow;

            await _positionStore.SaveAsync(position, cancellationToken);

            _logger.LogError(
                ex,
                "BOT8011 TP handling failed after TP fill. Position={ShortId}, RemainingQty={RemainingQuantity}",
                shortId,
                remainingQuantity);

            throw;
        }
    }

    public async Task HandleSlTriggeredAsync(
        string shortId,
        CancellationToken cancellationToken)
    {
        var position = await _positionStore.GetAsync(
            _options.BotName,
            shortId,
            cancellationToken);

        if (position is null || position.Closed)
            return;

        position.SlExecuted = true;
        position.SlStatus = "FILLED";
        position.SlTriggeredAtUtc = DateTime.UtcNow;
        position.Stop3Pending = false;
        position.TrailingInProgress = false;
        position.ProtectiveActive = false;

        await MarkClosedAsync(position, "SL_TRIGGERED", cancellationToken);
    }

    public async Task HandleStop3TriggeredAsync(
        string shortId,
        CancellationToken cancellationToken)
    {
        var position = await _positionStore.GetAsync(
            _options.BotName,
            shortId,
            cancellationToken);

        if (position is null || position.Closed)
            return;

        position.Stop3Status = "FILLED";
        position.Stop3TriggeredAtUtc = DateTime.UtcNow;
        position.Stop3Pending = false;
        position.TrailingInProgress = false;
        position.ProtectiveActive = false;

        await MarkClosedAsync(position, "STOP3_TRIGGERED", cancellationToken);
    }

    private async Task MarkClosedAsync(
        BotPosition position,
        string reason,
        CancellationToken cancellationToken)
    {
        position.Closed = true;
        position.Status = PositionStatus.Closed;
        position.ProtectiveActive = false;
        position.ClosedAtUtc = DateTime.UtcNow;
        position.UpdatedAtUtc = DateTime.UtcNow;

        await _positionStore.SaveAsync(position, cancellationToken);

        _logger.LogInformation(
            "BOT8011 position closed. Position={ShortId}, Reason={Reason}",
            position.ShortId,
            reason);
    }

    private decimal CalculateInitialStop3Price(BotPosition position)
    {
        if (!position.EntryPrice.HasValue)
            throw new InvalidOperationException($"Position {position.ShortId} has no entry price.");

        var offset = _options.Stop3EntryOffset;

        return position.Side == PositionSide.Long
            ? position.EntryPrice.Value + offset
            : position.EntryPrice.Value - offset;
    }

    private static string CreateClientId(string botName, string prefix, string shortId)
    {
        var shortBot = botName.Length > 8 ? botName[..8] : botName;
        var value = $"{shortBot}_{prefix}_{shortId}";

        return value[..Math.Min(32, value.Length)];
    }

    private static string ToCloseSide(PositionSide side)
        => side == PositionSide.Long ? "SELL" : "BUY";

    private static string ToPositionSide(PositionSide side)
        => side == PositionSide.Long ? "LONG" : "SHORT";
}