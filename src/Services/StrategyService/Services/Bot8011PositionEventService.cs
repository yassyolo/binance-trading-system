using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Execution;
using StrategyService.Infrastructure.Locking;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Orders;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Services;

public sealed class Bot8011PositionEventService(
    IOptions<Bot8011Options> options,
    IPositionStore positionStore,
    Bot8011Stop3OrderService stop3Orders,
    SafeBinanceOrderService safeOrders,
    PositionLockService locks,
    TelegramNotificationService telegram,
    ILogger<Bot8011PositionEventService> logger)
{
    private readonly Bot8011Options _options = options.Value;

    public async Task HandleTpFilledAsync(
        string shortId,
        decimal executedQuantity,
        CancellationToken cancellationToken)
    {
        if (!await locks.TryAcquireAsync(
                _options.BotName,
                shortId,
                TimeSpan.FromSeconds(20)))
            return;

        try
        {
            var position = await LoadAsync(shortId, cancellationToken);
            if (position is null || position.TpExecuted)
                return;

            position.MarkTpFilled(executedQuantity);
            position.TpStatus = "FILLED";
            position.TpFilledAtUtc = DateTime.UtcNow;

            if (position.RemainingQuantity <= 0)
            {
                await CancelInitialSlAsync(position, cancellationToken);
                position.ProtectiveActive = false;
                position.MarkClosed("TP_FULL_EXIT");
                await positionStore.SaveAsync(position, cancellationToken);
                return;
            }

            position.Stop3Pending = true;
            position.ProtectiveActive = true;
            position.Status = PositionStatus.Stop3Pending;
            position.UpdatedAtUtc = DateTime.UtcNow;
            await positionStore.SaveAsync(position, cancellationToken);

            try
            {
                // Safety-critical order:
                // create and persist STOP3 before canceling the initial SL.
                var stop3 = await stop3Orders.CreateStop3WithFallbackAsync(
                    position,
                    position.RemainingQuantity,
                    BinanceClientOrderIdFactory.Create(
                        _options.BotName,
                        "STOP3",
                        position.ShortId),
                    cancellationToken);

                position.Stop3ClientId = stop3.ClientAlgoId;
                position.Stop3OrderId = stop3.AlgoOrderId;
                position.Stop3Initial = stop3.TriggerPrice;
                position.Stop3Current = stop3.TriggerPrice;
                position.Stop3Previous = stop3.TriggerPrice;
                position.Stop3Status = stop3.Status;
                position.Stop3Created = true;
                position.Stop3Pending = false;
                position.ProtectiveActive = true;
                position.Status = PositionStatus.Stop3Active;
                position.UpdatedAtUtc = DateTime.UtcNow;

                await positionStore.SaveAsync(position, cancellationToken);

                await CancelInitialSlAsync(position, cancellationToken);
                await positionStore.SaveAsync(position, cancellationToken);

                await telegram.SendAsync(
                    $"🎯 {_options.BotName} TP filled\n" +
                    $"ID: {position.ShortId}\n" +
                    $"Remaining: {position.RemainingQuantity}\n" +
                    $"STOP3: {position.Stop3Current}",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // Initial SL remains active when STOP3 creation fails.
                position.Stop3Pending = true;
                position.Stop3Created = false;
                position.ProtectiveActive = true;
                position.Status = PositionStatus.Stop3Pending;
                position.UpdatedAtUtc = DateTime.UtcNow;

                await positionStore.SaveAsync(position, cancellationToken);

                logger.LogError(
                    ex,
                    "BOT8011 STOP3 creation failed. Initial SL remains active. Position={ShortId}",
                    position.ShortId);
            }
        }
        finally
        {
            await locks.ReleaseAsync(_options.BotName, shortId);
        }
    }

    public async Task HandleSlTriggeredAsync(
        string shortId,
        CancellationToken cancellationToken)
    {
        var position = await LoadAsync(shortId, cancellationToken);
        if (position is null)
            return;

        position.SlExecuted = true;
        position.SlStatus = "FILLED";
        position.SlTriggeredAtUtc = DateTime.UtcNow;
        position.ProtectiveActive = false;
        position.Stop3Pending = false;
        position.TrailingInProgress = false;
        position.MarkClosed("SL_TRIGGERED");

        await positionStore.SaveAsync(position, cancellationToken);
    }

    public async Task HandleStop3TriggeredAsync(
        string shortId,
        CancellationToken cancellationToken)
    {
        var position = await LoadAsync(shortId, cancellationToken);
        if (position is null)
            return;

        position.Stop3Status = "FILLED";
        position.Stop3TriggeredAtUtc = DateTime.UtcNow;
        position.ProtectiveActive = false;
        position.Stop3Pending = false;
        position.TrailingInProgress = false;
        position.MarkClosed("STOP3_TRIGGERED");

        await positionStore.SaveAsync(position, cancellationToken);
    }

    private async Task<BotPosition?> LoadAsync(
        string shortId,
        CancellationToken cancellationToken)
    {
        var position = await positionStore.GetAsync(
            _options.BotName,
            shortId,
            cancellationToken);

        return position is null || position.Closed ? null : position;
    }

    private async Task CancelInitialSlAsync(
        BotPosition position,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(position.SlOrderId)
            || position.SlExecuted
            || position.SlStatus is "CANCELED" or "CANCELLED")
            return;

        await safeOrders.SafeCancelAlgoAsync(
            position.Symbol,
            position.SlOrderId,
            position.SlClientId,
            cancellationToken);

        position.SlStatus = "CANCELED";
        position.UpdatedAtUtc = DateTime.UtcNow;
    }
}
