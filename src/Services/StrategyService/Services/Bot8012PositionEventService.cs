using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;

namespace StrategyService.Services;

public sealed class Bot8012PositionEventService
{
    private readonly Bot8012Options _options;
    private readonly IPositionStore _positionStore;
    private readonly ILogger<Bot8012PositionEventService> _logger;
    private readonly TelegramNotificationService _telegram;
    public Bot8012PositionEventService(
        IOptions<Bot8012Options> options,
        IPositionStore positionStore,
        ILogger<Bot8012PositionEventService> logger,
        TelegramNotificationService telegram)
    {
        _options = options.Value;
        _positionStore = positionStore;
        _logger = logger;
        _telegram = telegram;
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

        if (position is null)
            return;

        await _positionStore.DeleteAsync(
            _options.BotName,
            shortId,
            cancellationToken);

        await _telegram.SendAsync(
            $"✅ BOT8012 TP FILLED\nSide: {position.Side}\nShortId: {shortId}\nQty: {executedQuantity}\nTP: {position.TpPrice}",
            cancellationToken);

        _logger.LogInformation(
            "BOT8012 TP filled. Redis position deleted. Position={ShortId}, ExecutedQuantity={ExecutedQuantity}",
            shortId,
            executedQuantity);
    }

    public async Task HandleTpTerminalAsync(
        string shortId,
        string status,
        CancellationToken cancellationToken)
    {
        var position = await _positionStore.GetAsync(
            _options.BotName,
            shortId,
            cancellationToken);

        if (position is null || position.Closed)
            return;

        position.TpStatus = status;
        position.UpdatedAtUtc = DateTime.UtcNow;

        if (status is "CANCELED" or "EXPIRED" or "REJECTED")
            position.ProtectiveActive = false;

        await _positionStore.SaveAsync(position, cancellationToken);

        _logger.LogWarning(
            "BOT8012 TP terminal status. Position={ShortId}, Status={Status}",
            shortId,
            status);
    }
}