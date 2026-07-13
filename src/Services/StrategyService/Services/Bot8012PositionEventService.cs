using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;

namespace StrategyService.Services;

public sealed class Bot8012PositionEventService(
        IOptions<Bot8012Options> options,
        IPositionStore positionStore,
        ILogger<Bot8012PositionEventService> logger,
        TelegramNotificationService telegram)
{
    private readonly Bot8012Options options = options.Value;
    public async Task HandleTpFilledAsync(
    string shortId,
    decimal executedQuantity,
    CancellationToken cancellationToken)
    {
        var position = await positionStore.GetAsync(
            options.BotName,
            shortId,
            cancellationToken);

        if (position is null)
            return;

        await positionStore.DeleteAsync(
            options.BotName,
            shortId,
            cancellationToken);

        await telegram.SendAsync(
            $"✅ BOT8012 TP FILLED\nSide: {position.Side}\nShortId: {shortId}\nQty: {executedQuantity}\nTP: {position.TpPrice}",
            cancellationToken);

        logger.LogInformation(
            "BOT8012 TP filled. Redis position deleted. Position={ShortId}, ExecutedQuantity={ExecutedQuantity}",
            shortId,
            executedQuantity);
    }

    public async Task HandleTpTerminalAsync(
        string shortId,
        string status,
        CancellationToken cancellationToken)
    {
        var position = await positionStore.GetAsync(
            options.BotName,
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
            "BOT8012 TP terminal status. Position={ShortId}, Status={Status}",
            shortId,
            status);
    }
}