using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;

namespace StrategyService.Services;

public sealed class Bot8012PositionEventService
{
    private readonly Bot8012Options _options;
    private readonly IPositionStore _positionStore;
    private readonly ILogger<Bot8012PositionEventService> _logger;

    public Bot8012PositionEventService(
        IOptions<Bot8012Options> options,
        IPositionStore positionStore,
        ILogger<Bot8012PositionEventService> logger)
    {
        _options = options.Value;
        _positionStore = positionStore;
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

        position.TpExecuted = true;
        position.TpStatus = "FILLED";
        position.TpFilledAtUtc = DateTime.UtcNow;
        position.RemainingQuantity = Math.Max(position.RemainingQuantity - executedQuantity, 0);
        position.MarkClosed("TP_FILLED");

        await _positionStore.SaveAsync(position, cancellationToken);

        _logger.LogInformation(
            "BOT8012 TP filled. Position={ShortId}, ExecutedQuantity={ExecutedQuantity}",
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