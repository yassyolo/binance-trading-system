using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Time;

namespace StrategyService.Services;

public sealed class Bot8014PositionEventService(
    IOptions<Bot8014Options> options,
    IPositionStore positionStore,
    IClock clock,
    ILogger<Bot8014PositionEventService> logger,
    TelegramNotificationService telegram)
{
    private readonly Bot8014Options _options = options.Value;

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
        {
            logger.LogWarning(
                "BOT8014 TP fill received for unknown position. ShortId={ShortId}, Qty={Qty}",
                shortId,
                executedQuantity);

            return;
        }

        if (position.Closed)
            return;

        position.TpExecuted = true;
        position.TpStatus = "FILLED";
        position.RemainingQuantity = Math.Max(
            0,
            position.RemainingQuantity - executedQuantity);
        position.TpFilledAtUtc = clock.UtcNow;
        position.ProtectiveActive = false;
        position.MarkClosed("TP_FILLED");

        await positionStore.SaveAsync(
            position,
            cancellationToken);

        await telegram.SendAsync(
            $"✅ {_options.BotName} TP_FILLED\n" +
            $"Side: {position.Side}\n" +
            $"ShortId: {shortId}\n" +
            $"Qty: {executedQuantity}\n" +
            $"TP: {position.TpPrice}",
            cancellationToken);

        logger.LogInformation(
            "BOT8014 TP filled and position finalized. ShortId={ShortId}, Qty={Qty}",
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
        position.UpdatedAtUtc = clock.UtcNow;

        if (IsTerminalNonFilled(status))
            position.ProtectiveActive = false;

        await positionStore.SaveAsync(
            position,
            cancellationToken);

        await telegram.SendAsync(
            $"⚠️ {_options.BotName} TP_{status}\n" +
            $"Side: {position.Side}\n" +
            $"ShortId: {shortId}\n" +
            $"Position requires reconciliation.",
            cancellationToken);

        logger.LogWarning(
            "BOT8014 TP reached terminal non-filled status. ShortId={ShortId}, Status={Status}. Position was preserved for reconciliation.",
            shortId,
            status);
    }

    private static bool IsTerminalNonFilled(string status)
        => status.Equals("CANCELED", StringComparison.OrdinalIgnoreCase)
           || status.Equals("EXPIRED", StringComparison.OrdinalIgnoreCase)
           || status.Equals("REJECTED", StringComparison.OrdinalIgnoreCase);
}
