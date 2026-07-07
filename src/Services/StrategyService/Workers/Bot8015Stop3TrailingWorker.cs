using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Market.Contracts;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Workers;

public sealed class Bot8015Stop3TrailingWorker : BackgroundService
{
    private readonly Bot8015Options _options;
    private readonly IPositionStore _positionStore;
    private readonly IBinanceFuturesOrderClient _orders;
    private readonly IBinanceFuturesMarketClient _market; private readonly TelegramNotificationService _telegram;
    private readonly ILogger<Bot8015Stop3TrailingWorker> _logger;

    public Bot8015Stop3TrailingWorker(
        IOptions<Bot8015Options> options,
        IPositionStore positionStore,
        IBinanceFuturesOrderClient orders,
        IBinanceFuturesMarketClient marketData,
        TelegramNotificationService telegram,
        ILogger<Bot8015Stop3TrailingWorker> logger)
    {
        _options = options.Value;
        _positionStore = positionStore;
        _orders = orders;
        _market = marketData;
        _telegram = telegram;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableTrailing)
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTrailingAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BOT8015 Stop3 trailing worker failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessTrailingAsync(CancellationToken cancellationToken)
    {
        var markPrice = await _market.GetMarkPriceAsync(
            _options.Symbol,
            cancellationToken);

        if (markPrice <= 0)
            return;

        var positions = await _positionStore.GetAllAsync(
            _options.BotName,
            cancellationToken);

        foreach (var position in positions)
        {
            if (!ShouldTrail(position))
                continue;

            await TryTrailPositionAsync(position, markPrice, cancellationToken);
        }
    }

    private bool ShouldTrail(BotPosition position)
    {
        return !position.Closed
               && position.Symbol == _options.Symbol
               && position.TpExecuted
               && position.ProtectiveActive
               && position.Stop3Created
               && !position.Stop3Pending
               && !position.TrailingInProgress
               && position.RemainingQuantity > 0
               && position.Stop3Current > 0;
    }

    private async Task TryTrailPositionAsync(
        BotPosition position,
        decimal markPrice,
        CancellationToken cancellationToken)
    {
        var currentStop3 = position.Stop3Current!.Value;

        var shouldMove = position.Side switch
        {
            PositionSide.Long => markPrice >= currentStop3 + _options.Stop3TrailingStep,
            PositionSide.Short => markPrice <= currentStop3 - _options.Stop3TrailingStep,
            _ => false
        };

        if (!shouldMove)
            return;

        var newStop3 = position.Side switch
        {
            PositionSide.Long => currentStop3 + _options.Stop3TrailingBuffer,
            PositionSide.Short => currentStop3 - _options.Stop3TrailingBuffer,
            _ => currentStop3
        };

        if (!IsImprovedStop(position.Side, currentStop3, newStop3))
            return;

        position.TrailingInProgress = true;
        position.Stop3NewPending = newStop3;
        position.Stop3Previous = currentStop3;
        position.UpdatedAtUtc = DateTime.UtcNow;

        await _positionStore.SaveAsync(position, cancellationToken);

        try
        {
            await CancelOldStop3Async(position, cancellationToken);

            var newClientId = CreateStop3ClientId(
                _options.BotName,
                position.ShortId,
                position.TrailCount + 1);

            var newOrder = await _orders.PlaceStopMarketAlgoOrderAsync(
                position.Symbol,
                ToCloseSide(position.Side),
                ToPositionSide(position.Side),
                position.RemainingQuantity,
                newStop3,
                newClientId,
                cancellationToken);

            position.Stop3ClientId = newClientId;
            position.Stop3OrderId = newOrder.AlgoOrderId;
            position.Stop3Current = newStop3;
            position.Stop3Status = newOrder.Status;
            position.Stop3NewPending = null;
            position.TrailingInProgress = false;
            position.Stop3Pending = false;
            position.ProtectiveActive = true;
            position.TrailCount += 1;
            position.UpdatedAtUtc = DateTime.UtcNow;

            await _positionStore.SaveAsync(position, cancellationToken);

            await _telegram.SendAsync(
                $"🔄 BOT8015 STOP3 Trailing\nID: {position.ShortId}\nSide: {position.Side}\nOld: {currentStop3}\nNew: {newStop3}\nMark: {markPrice}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            position.TrailingInProgress = false;
            position.Stop3NewPending = null;
            position.UpdatedAtUtc = DateTime.UtcNow;

            await _positionStore.SaveAsync(position, cancellationToken);

            _logger.LogError(
                ex,
                "BOT8015 STOP3 trailing failed. ShortId={ShortId}",
                position.ShortId);
        }
    }

    private async Task CancelOldStop3Async(
        BotPosition position,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(position.Stop3OrderId))
            return;

        await _orders.CancelAlgoOrderAsync(
            position.Symbol,
            position.Stop3OrderId,
            cancellationToken);
    }

    private static bool IsImprovedStop(
        PositionSide side,
        decimal currentStop,
        decimal newStop)
    {
        return side switch
        {
            PositionSide.Long => newStop > currentStop,
            PositionSide.Short => newStop < currentStop,
            _ => false
        };
    }

    private static string CreateStop3ClientId(
        string botName,
        string shortId,
        int trailCount)
    {
        var shortBot = botName.Length > 8
            ? botName[..8]
            : botName;

        var clientId = $"{shortBot}_STOP3_{shortId}_T{trailCount}";

        return clientId.Length <= 32
            ? clientId
            : clientId[..32];
    }

    private static string ToCloseSide(PositionSide side)
        => side == PositionSide.Long ? "SELL" : "BUY";

    private static string ToPositionSide(PositionSide side)
        => side == PositionSide.Long ? "LONG" : "SHORT";
}