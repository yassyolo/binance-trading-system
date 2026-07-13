using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Orders.Contracts;

namespace StrategyService.Services;

public sealed class Bot8015PositionEventService(
    IOptions<Bot8015Options> options,
    IPositionStore positionStore,
    Bot8015Stop3OrderService stop3Orders,
    IBinanceFuturesOrderClient orders,
    IClock clock,
    TelegramNotificationService telegram,
    ILogger<Bot8015PositionEventService> logger)
{
    private readonly Bot8015Options _options = options.Value;

    public async Task HandleTpFilledAsync(
        string shortId,
        decimal executedQuantity,
        CancellationToken cancellationToken)
    {
        var position = await positionStore.GetAsync(
            _options.BotName,
            shortId,
            cancellationToken);

        if (position is null || position.Closed || position.TpExecuted)
            return;

        position.MarkTpFilled(executedQuantity);
        position.TpStatus = "FILLED";
        position.TpFilledAtUtc = clock.UtcNow;

        if (position.RemainingQuantity <= 0)
        {
            await CancelInitialSlAsync(position, cancellationToken);

            position.ProtectiveActive = false;
            position.MarkClosed("TP_FULL_EXIT");

            await positionStore.SaveAsync(
                position,
                cancellationToken);

            return;
        }

        position.ProtectiveActive = true;
        position.Stop3Pending = true;
        position.UpdatedAtUtc = clock.UtcNow;

        await positionStore.SaveAsync(
            position,
            cancellationToken);

        try
        {
            await stop3Orders.CreateAfterTpAsync(
                position,
                cancellationToken);

            await CancelInitialSlAsync(
                position,
                cancellationToken);

            position.SlStatus = "CANCELED";
            position.Stop3Created = true;
            position.Stop3Pending = false;
            position.ProtectiveActive = true;
            position.UpdatedAtUtc = clock.UtcNow;

            await positionStore.SaveAsync(
                position,
                cancellationToken);

            await telegram.SendAsync(
                $"🎯 {_options.BotName} TP filled\n" +
                $"ID: {shortId}\n" +
                $"Executed: {executedQuantity}\n" +
                $"Remaining: {position.RemainingQuantity}\n" +
                $"STOP3: {position.Stop3Current}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            position.Stop3Created = false;
            position.Stop3Pending = true;
            position.ProtectiveActive = true;
            position.UpdatedAtUtc = clock.UtcNow;

            await positionStore.SaveAsync(
                position,
                cancellationToken);

            logger.LogError(
                ex,
                "BOT8015 failed to create STOP3 after TP. Initial SL was preserved. ShortId={ShortId}",
                shortId);

            throw;
        }
    }

    public async Task HandleSlTriggeredAsync(
        string shortId,
        CancellationToken cancellationToken)
    {
        var position = await positionStore.GetAsync(
            _options.BotName,
            shortId,
            cancellationToken);

        if (position is null || position.Closed)
            return;

        position.SlExecuted = true;
        position.SlStatus = "FILLED";
        position.ProtectiveActive = false;
        position.MarkClosed("SL_ALGO_TRIGGERED");

        await CancelTpIfOpenAsync(
            position,
            cancellationToken);

        await positionStore.SaveAsync(
            position,
            cancellationToken);

        await telegram.SendAsync(
            $"🛑 {_options.BotName} SL TRIGGERED\n" +
            $"ID: {shortId}\n" +
            $"Side: {position.Side}",
            cancellationToken);
    }

    public async Task HandleStop3TriggeredAsync(
        string shortId,
        CancellationToken cancellationToken)
    {
        var position = await positionStore.GetAsync(
            _options.BotName,
            shortId,
            cancellationToken);

        if (position is null || position.Closed)
            return;

        position.Stop3Status = "FILLED";
        position.ProtectiveActive = false;
        position.TrailingInProgress = false;
        position.Stop3NewPending = null;
        position.MarkClosed("STOP3_ALGO_TRIGGERED");

        await positionStore.SaveAsync(
            position,
            cancellationToken);

        await telegram.SendAsync(
            $"🛑 {_options.BotName} STOP3 TRIGGERED\n" +
            $"ID: {shortId}\n" +
            $"Side: {position.Side}",
            cancellationToken);
    }

    public async Task HandleTpTerminalAsync(
        string shortId,
        string status,
        CancellationToken cancellationToken)
    {
        var position = await positionStore.GetAsync(
            _options.BotName,
            shortId,
            cancellationToken);

        if (position is null || position.Closed)
            return;

        position.TpStatus = status;
        position.UpdatedAtUtc = clock.UtcNow;

        await positionStore.SaveAsync(
            position,
            cancellationToken);
    }

    private async Task CancelInitialSlAsync(
        TradingSystem.Domain.Positions.BotPosition position,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(position.SlOrderId)
            || position.SlExecuted)
        {
            return;
        }

        await orders.CancelAlgoOrderAsync(
            position.Symbol,
            position.SlOrderId,
            cancellationToken);
    }

    private async Task CancelTpIfOpenAsync(
        TradingSystem.Domain.Positions.BotPosition position,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(position.TpOrderId)
            || position.TpExecuted)
        {
            return;
        }

        await orders.CancelOrderAsync(
            position.Symbol,
            position.TpOrderId,
            cancellationToken);
    }
}
