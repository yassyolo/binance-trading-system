using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;

namespace StrategyService.Services;

public sealed class Bot8013PositionEventService(
    IOptions<Bot8013Options> options,
    IPositionStore positionStore,
    ILogger<Bot8013PositionEventService> logger,
    TelegramNotificationService telegram)
{
    private readonly Bot8013Options _options = options.Value;

    public async Task HandleTpFilledAsync(
        string shortId,
        decimal executedQuantity,
        CancellationToken cancellationToken)
    {
        var position = await positionStore.GetAsync(
            _options.BotName,
            shortId,
            cancellationToken);

        if (position is null)
            return;

        await positionStore.DeleteAsync(
            _options.BotName,
            shortId,
            cancellationToken);

        await telegram.SendAsync(
            $"✅ BOT8013 TP_FILLED\nSide: {position.Side}\nShortId: {shortId}\nQty: {executedQuantity}\nTP: {position.TpPrice}",
            cancellationToken);

        logger.LogInformation(
            "BOT8013 TP filled. Redis position deleted. ShortId={ShortId}, Qty={Qty}",
            shortId,
            executedQuantity);
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
        position.UpdatedAtUtc = DateTime.UtcNow;

        if (status is "CANCELED" or "EXPIRED" or "REJECTED")
            position.ProtectiveActive = false;

        await positionStore.SaveAsync(position, cancellationToken);

        logger.LogWarning(
            "BOT8013 TP terminal status. ShortId={ShortId}, Status={Status}",
            shortId,
            status);
    }
}