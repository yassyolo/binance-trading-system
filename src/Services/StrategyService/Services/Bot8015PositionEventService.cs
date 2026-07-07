using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Domain.Positions;

namespace StrategyService.Services;

public sealed class Bot8015PositionEventService(
    IOptions<Bot8015Options> options,
    IPositionStore positionStore,
    Bot8011Stop3OrderService stop3Orders,
    TelegramNotificationService telegram,
    ILogger<Bot8015PositionEventService> logger)
{
    private readonly Bot8015Options _options = options.Value;

    public async Task HandleTpFilledAsync(
        string shortId,
        decimal executedQuantity,
        CancellationToken cancellationToken)
    {
        var position = await positionStore.GetAsync(_options.BotName, shortId, cancellationToken);

        if (position is null || position.Closed)
            return;

        position.MarkTpFilled(executedQuantity);
        position.ProtectiveActive = true;
        position.Stop3Pending = true;

        await positionStore.SaveAsync(position, cancellationToken);

        await stop3Orders.CreateStop3AfterTpAsync(
            _options.BotName,
            position,
            _options.Stop3EntryOffset,
            cancellationToken);

        position.Stop3Created = true;
        position.Stop3Pending = false;
        position.ProtectiveActive = true;

        await positionStore.SaveAsync(position, cancellationToken);

        await telegram.SendAsync(
            $"🎯 BOT8015 TP filled\nID: {shortId}\nExecuted: {executedQuantity}\nRemaining: {position.RemainingQuantity}\nSTOP3 created.",
            cancellationToken);

        logger.LogInformation(
            "BOT8015 TP processed. ShortId={ShortId}, Remaining={Remaining}",
            shortId,
            position.RemainingQuantity);
    }

   

    public async Task HandleSlTriggeredAsync(
        string shortId,
        CancellationToken cancellationToken)
    {
        var position = await positionStore.GetAsync(_options.BotName, shortId, cancellationToken);

        if (position is null)
            return;

        position.SlExecuted = true;
        position.SlStatus = "FILLED";
        position.ProtectiveActive = false;
        position.MarkClosed("SL algo triggered");

        await positionStore.DeleteAsync(_options.BotName, shortId, cancellationToken);

        await telegram.SendAsync(
            $"🛑 BOT8015 SL TRIGGERED\nID: {shortId}\nSide: {position.Side}",
            cancellationToken);
    }

    public async Task HandleStop3TriggeredAsync(
        string shortId,
        CancellationToken cancellationToken)
    {
        var position = await positionStore.GetAsync(_options.BotName, shortId, cancellationToken);

        if (position is null)
            return;

        position.Stop3Status = "FILLED";
        position.ProtectiveActive = false;
        position.TrailingInProgress = false;
        position.MarkClosed("STOP3 algo triggered");

        await positionStore.DeleteAsync(_options.BotName, shortId, cancellationToken);

        await telegram.SendAsync(
            $"🛑 BOT8015 STOP3 TRIGGERED\nID: {shortId}\nSide: {position.Side}",
            cancellationToken);
    }

    public async Task HandleTpTerminalAsync(
        string shortId,
        string status,
        CancellationToken cancellationToken)
    {
        var position = await positionStore.GetAsync(_options.BotName, shortId, cancellationToken);

        if (position is null || position.Closed)
            return;

        position.TpStatus = status;
        position.UpdatedAtUtc = DateTime.UtcNow;

        await positionStore.SaveAsync(position, cancellationToken);
    }
}